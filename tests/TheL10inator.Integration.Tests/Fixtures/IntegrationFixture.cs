using System.Reflection;
using DbUp;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Respawn;
using Testcontainers.MsSql;

namespace TheL10inator.Integration.Tests.Fixtures;

/// <summary>
/// Shared Testcontainers + Respawn fixture that boots a real SQL Server container,
/// runs every embedded DbUp migration, and hands the resulting connection string to the
/// <see cref="WebApplicationFactory{TEntryPoint}"/>-hosted Api. Tests call
/// <see cref="ResetAsync"/> between cases to return the database to a clean state.
/// </summary>
public sealed class IntegrationFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer;
    private Respawner? _respawner;

    public IntegrationFixture()
    {
        _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Your_strong_password123")
            .Build();
    }

    public string ConnectionString { get; private set; } = string.Empty;

    public TheL10inatorWebApplicationFactory Factory { get; private set; } = null!;

    public const string SeededAdminEmail = "seeded-admin@example.com";

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        // Resolve the app-scope connection string against the `TheL10inator` database rather
        // than `master` so migrations and queries target the same catalog.
        var builder = new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString())
        {
            InitialCatalog = "TheL10inator",
            TrustServerCertificate = true,
        };
        ConnectionString = builder.ConnectionString;

        // Load the Migrator assembly so DbUp picks up its embedded *.sql resources.
        var migratorAssembly = Assembly.Load("TheL10inator.Migrator");

        EnsureDatabase.For.SqlDatabase(ConnectionString);
        var upgrader = DeployChanges.To
            .SqlDatabase(ConnectionString)
            .WithScriptsEmbeddedInAssembly(migratorAssembly)
            .LogToNowhere()
            .Build();
        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException("Migration failed during IntegrationFixture.InitializeAsync.", result.Error);
        }

        Factory = new TheL10inatorWebApplicationFactory(ConnectionString, SeededAdminEmail);
        // Trigger the host build so Program.cs's startup seed runs once against the migrated DB.
        _ = Factory.Services.GetRequiredService<IHostEnvironment>();

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            TablesToIgnore = new[] { new Respawn.Graph.Table("SchemaVersions") },
        });
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }

    public async Task ResetAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner!.ResetAsync(connection);
    }
}

[CollectionDefinition(nameof(IntegrationCollection))]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationFixture>
{
}

file static class ServiceProviderExtensions
{
    public static T GetRequiredService<T>(this IServiceProvider services) where T : notnull =>
        (T)services.GetService(typeof(T))!;
}
