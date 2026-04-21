using Microsoft.Extensions.DependencyInjection;
using TheL10inator.Domain.Interfaces;
using TheL10inator.Infrastructure.Repositories;
using TheL10inator.Infrastructure.Sql;
using TheL10inator.Infrastructure.Startup;

namespace TheL10inator.Infrastructure;

/// <summary>
/// Convenience extension that registers every repository, the connection factory, and the
/// admin seeder. Api wires it up exactly once from <c>Program.cs</c>.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddTheL10inatorInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string? firstAdminEmail)
    {
        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connectionString));

        services.AddScoped<ITeamRepository, TeamRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();

        services.AddScoped<IAdminSeeder>(sp => new AdminSeeder(
            sp.GetRequiredService<ISqlConnectionFactory>(),
            firstAdminEmail,
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AdminSeeder>>()));

        return services;
    }
}
