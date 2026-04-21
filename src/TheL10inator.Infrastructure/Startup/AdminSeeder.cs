using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using TheL10inator.Domain.Interfaces;
using TheL10inator.Domain.Models;
using TheL10inator.Infrastructure.Sql;

namespace TheL10inator.Infrastructure.Startup;

/// <summary>
/// Seeds the Leadership team and its first admin user on startup when no admin team
/// member exists yet. Inserts all three rows (team, user, team member) inside a single
/// transaction so startup can never leave the database in a half-seeded state.
/// </summary>
/// <remarks>
/// Two constructors exist:
/// <list type="bullet">
/// <item>The production overload takes an <see cref="ISqlConnectionFactory"/> and performs the
/// writes inside a real SQL transaction. This is what <c>Program.cs</c> wires up.</item>
/// <item>The unit-test overload takes the three repository interfaces directly, which lets
/// tests assert the seed effect using in-memory fakes without a database.</item>
/// </list>
/// The two paths preserve the same observable outcome — one team, one user, one admin-role
/// team member — so the unit-test path faithfully models the production behavior.
/// </remarks>
public sealed class AdminSeeder : IAdminSeeder
{
    /// <summary>The team name inserted when the seeder runs.</summary>
    public const string LeadershipTeamName = "Leadership";

    private readonly string? _firstAdminEmail;
    private readonly ILogger<AdminSeeder> _logger;
    private readonly ISqlConnectionFactory? _connectionFactory;
    private readonly ITeamRepository? _teamRepository;
    private readonly IUserRepository? _userRepository;
    private readonly ITeamMemberRepository? _teamMemberRepository;

    /// <summary>Production constructor — writes inside a single SQL transaction.</summary>
    public AdminSeeder(
        ISqlConnectionFactory connectionFactory,
        string? firstAdminEmail,
        ILogger<AdminSeeder> logger)
    {
        _connectionFactory = connectionFactory;
        _firstAdminEmail = firstAdminEmail;
        _logger = logger;
    }

    /// <summary>Test constructor — delegates to repository fakes, no transaction.</summary>
    public AdminSeeder(
        ITeamRepository teamRepository,
        IUserRepository userRepository,
        ITeamMemberRepository teamMemberRepository,
        string? firstAdminEmail,
        ILogger<AdminSeeder> logger)
    {
        _teamRepository = teamRepository;
        _userRepository = userRepository;
        _teamMemberRepository = teamMemberRepository;
        _firstAdminEmail = firstAdminEmail;
        _logger = logger;
    }

    public async Task SeedIfMissingAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_firstAdminEmail))
        {
            throw new InvalidOperationException(
                "Administration:FirstAdminEmail is required when no admin team member exists. " +
                "Set it via appsettings.json or the Administration__FirstAdminEmail environment variable.");
        }

        if (_connectionFactory is not null)
        {
            await SeedViaSqlAsync(_firstAdminEmail, ct).ConfigureAwait(false);
        }
        else
        {
            await SeedViaRepositoriesAsync(_firstAdminEmail, ct).ConfigureAwait(false);
        }
    }

    private async Task SeedViaSqlAsync(string firstAdminEmail, CancellationToken ct)
    {
        using var connection = await _connectionFactory!.OpenAsync(ct).ConfigureAwait(false);

        var adminExists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.TeamMembers WHERE DeletedAtUtc IS NULL AND Role = 'Admin') THEN 1 ELSE 0 END;",
                cancellationToken: ct))
            .ConfigureAwait(false);

        if (adminExists == 1)
        {
            _logger.LogInformation("First-admin seed skipped: an Admin team member already exists.");
            return;
        }

        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        var teamId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "INSERT INTO dbo.Teams (Name) OUTPUT INSERTED.Id VALUES (@Name);",
            new { Name = LeadershipTeamName },
            transaction: transaction,
            cancellationToken: ct))
            .ConfigureAwait(false);

        var userId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "INSERT INTO dbo.Users (Email) OUTPUT INSERTED.Id VALUES (@Email);",
            new { Email = firstAdminEmail },
            transaction: transaction,
            cancellationToken: ct))
            .ConfigureAwait(false);

        var memberId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "INSERT INTO dbo.TeamMembers (TeamId, UserId, Role) OUTPUT INSERTED.Id VALUES (@TeamId, @UserId, @Role);",
            new { TeamId = teamId, UserId = userId, Role = TeamRole.Admin.ToString() },
            transaction: transaction,
            cancellationToken: ct))
            .ConfigureAwait(false);

        transaction.Commit();

        _logger.LogInformation(
            "Seeded first admin: TeamId={TeamId} UserId={UserId} TeamMemberId={TeamMemberId}",
            teamId, userId, memberId);
    }

    private async Task SeedViaRepositoriesAsync(string firstAdminEmail, CancellationToken ct)
    {
        if (await _teamMemberRepository!.HasAnyAdminAsync(ct).ConfigureAwait(false))
        {
            _logger.LogInformation("First-admin seed skipped: an Admin team member already exists.");
            return;
        }

        var teamId = await _teamRepository!.InsertAsync(LeadershipTeamName, ct).ConfigureAwait(false);
        var userId = await _userRepository!.InsertInvitedAsync(firstAdminEmail, ct).ConfigureAwait(false);
        var memberId = await _teamMemberRepository!.InsertAsync(teamId, userId, TeamRole.Admin, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Seeded first admin: TeamId={TeamId} UserId={UserId} TeamMemberId={TeamMemberId}",
            teamId, userId, memberId);
    }
}
