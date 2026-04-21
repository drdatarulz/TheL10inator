using System.Reflection;
using DbUp;
using Microsoft.Extensions.Configuration;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .Build();

    var connectionString = config.GetConnectionString("Sql")
        ?? throw new InvalidOperationException("ConnectionStrings:Sql is required.");

    EnsureDatabase.For.SqlDatabase(connectionString);

    var upgrader = DeployChanges.To
        .SqlDatabase(connectionString)
        .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
        .LogToConsole()
        .Build();

    var result = upgrader.PerformUpgrade();

    if (!result.Successful)
    {
        Log.Error(result.Error, "Migration failed");
        return 1;
    }

    Log.Information("Migration succeeded");
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Migrator aborted unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
