using Dapper;
using TheL10inator.Domain.Interfaces;
using TheL10inator.Domain.Models;
using TheL10inator.Infrastructure.Sql;

namespace TheL10inator.Infrastructure.Repositories;

/// <summary>
/// Dapper-backed <see cref="ITeamRepository"/>.
/// </summary>
public sealed class TeamRepository : ITeamRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TeamRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Team?> GetSingletonAsync(CancellationToken ct)
    {
        const string sql = @"
SELECT TOP (1) Id, Name, CreatedAtUtc, DeletedAtUtc
FROM dbo.Teams
WHERE DeletedAtUtc IS NULL
ORDER BY Id;";

        using var connection = await _connectionFactory.OpenAsync(ct).ConfigureAwait(false);
        return await connection.QuerySingleOrDefaultAsync<Team>(new CommandDefinition(sql, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    public async Task<int> InsertAsync(string name, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.Teams (Name)
OUTPUT INSERTED.Id
VALUES (@Name);";

        using var connection = await _connectionFactory.OpenAsync(ct).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { Name = name }, cancellationToken: ct))
            .ConfigureAwait(false);
    }
}
