using Dapper;
using TheL10inator.Domain.Interfaces;
using TheL10inator.Domain.Models;
using TheL10inator.Infrastructure.Sql;

namespace TheL10inator.Infrastructure.Repositories;

/// <summary>
/// Dapper-backed <see cref="IUserRepository"/>.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public UserRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByAzureAdObjectIdAsync(string objectId, CancellationToken ct)
    {
        const string sql = @"
SELECT Id, AzureAdObjectId, Email, DisplayName, InvitedAtUtc, LastLoginAtUtc, CreatedAtUtc, DeletedAtUtc
FROM dbo.Users
WHERE DeletedAtUtc IS NULL AND AzureAdObjectId = @ObjectId;";

        using var connection = await _connectionFactory.OpenAsync(ct).ConfigureAwait(false);
        return await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { ObjectId = objectId }, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        const string sql = @"
SELECT Id, AzureAdObjectId, Email, DisplayName, InvitedAtUtc, LastLoginAtUtc, CreatedAtUtc, DeletedAtUtc
FROM dbo.Users
WHERE DeletedAtUtc IS NULL AND Email = @Email;";

        using var connection = await _connectionFactory.OpenAsync(ct).ConfigureAwait(false);
        return await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    public async Task<int> InsertInvitedAsync(string email, CancellationToken ct)
    {
        const string sql = @"
INSERT INTO dbo.Users (Email)
OUTPUT INSERTED.Id
VALUES (@Email);";

        using var connection = await _connectionFactory.OpenAsync(ct).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: ct))
            .ConfigureAwait(false);
    }

    public async Task UpdateObjectIdAndLoginAsync(int userId, string objectId, string? displayName, CancellationToken ct)
    {
        const string sql = @"
UPDATE dbo.Users
SET AzureAdObjectId = @ObjectId,
    DisplayName = COALESCE(@DisplayName, DisplayName),
    LastLoginAtUtc = SYSUTCDATETIME()
WHERE Id = @UserId;";

        using var connection = await _connectionFactory.OpenAsync(ct).ConfigureAwait(false);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { ObjectId = objectId, DisplayName = displayName, UserId = userId },
            cancellationToken: ct))
            .ConfigureAwait(false);
    }
}
