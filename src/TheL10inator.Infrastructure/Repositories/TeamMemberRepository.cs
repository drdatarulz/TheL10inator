using Dapper;
using TheL10inator.Domain.Interfaces;
using TheL10inator.Domain.Models;
using TheL10inator.Infrastructure.Sql;

namespace TheL10inator.Infrastructure.Repositories;

/// <summary>
/// Dapper-backed <see cref="ITeamMemberRepository"/>. The <c>Role</c> column stores the
/// enum member name (<c>Member</c>/<c>Admin</c>) so a <c>CHECK</c> constraint can enforce
/// the allowed set at the database.
/// </summary>
public sealed class TeamMemberRepository : ITeamMemberRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TeamMemberRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> HasAnyAdminAsync(CancellationToken ct)
    {
        const string sql = @"
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM dbo.TeamMembers
    WHERE DeletedAtUtc IS NULL AND Role = 'Admin'
) THEN 1 ELSE 0 END;";

        using var connection = await _connectionFactory.OpenAsync(ct).ConfigureAwait(false);
        var flag = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, cancellationToken: ct))
            .ConfigureAwait(false);
        return flag == 1;
    }

    public async Task<TeamMember?> GetByUserIdAsync(int userId, CancellationToken ct)
    {
        const string sql = @"
SELECT Id, TeamId, UserId, Role, JoinedAtUtc, CreatedAtUtc, DeletedAtUtc
FROM dbo.TeamMembers
WHERE DeletedAtUtc IS NULL AND UserId = @UserId;";

        using var connection = await _connectionFactory.OpenAsync(ct).ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<TeamMemberRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct))
            .ConfigureAwait(false);
        return row?.ToDomain();
    }

    public async Task<int> InsertAsync(int teamId, int userId, TeamRole role, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.TeamMembers (TeamId, UserId, Role)
OUTPUT INSERTED.Id
VALUES (@TeamId, @UserId, @Role);";

        using var connection = await _connectionFactory.OpenAsync(ct).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { TeamId = teamId, UserId = userId, Role = role.ToString() }, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    private sealed record TeamMemberRow(
        int Id,
        int TeamId,
        int UserId,
        string Role,
        DateTime JoinedAtUtc,
        DateTime CreatedAtUtc,
        DateTime? DeletedAtUtc)
    {
        public TeamMember ToDomain() => new(
            Id,
            TeamId,
            UserId,
            Enum.Parse<TeamRole>(Role),
            JoinedAtUtc,
            CreatedAtUtc,
            DeletedAtUtc);
    }
}
