using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TheL10inator.Integration.Tests.Fixtures;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> wrapper that injects the Testcontainers
/// connection string and flips the Api into dev-bypass mode for the test run. Production
/// code paths remain under test; the only swap is how the caller is authenticated.
/// </summary>
public sealed class TheL10inatorWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _firstAdminEmail;

    public TheL10inatorWebApplicationFactory(string connectionString, string firstAdminEmail)
    {
        _connectionString = connectionString;
        _firstAdminEmail = firstAdminEmail;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Sql"] = _connectionString,
                ["Administration:FirstAdminEmail"] = _firstAdminEmail,
                ["Authentication:UseDevBypass"] = "true",
            });
        });
    }
}
