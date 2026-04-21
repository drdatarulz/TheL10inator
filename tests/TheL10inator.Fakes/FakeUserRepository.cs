using TheL10inator.Domain.Interfaces;
using TheL10inator.Domain.Models;

namespace TheL10inator.Fakes;

/// <summary>
/// In-memory implementation of <see cref="IUserRepository"/> for unit tests.
/// Records <see cref="UpdateObjectIdAndLoginAsync"/> calls so the middleware's bridging
/// behavior can be asserted without touching SQL.
/// </summary>
public sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new();
    private int _nextId = 1;

    public IReadOnlyList<User> Users => _users;

    public List<(int UserId, string ObjectId, string? DisplayName)> UpdateObjectIdAndLoginCalls { get; } = new();

    public Task<User?> GetByAzureAdObjectIdAsync(string objectId, CancellationToken ct)
    {
        var user = _users.FirstOrDefault(u =>
            u.DeletedAtUtc is null &&
            string.Equals(u.AzureAdObjectId, objectId, StringComparison.Ordinal));
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        var user = _users.FirstOrDefault(u =>
            u.DeletedAtUtc is null &&
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<int> InsertInvitedAsync(string email, CancellationToken ct)
    {
        var id = _nextId++;
        _users.Add(new User(
            Id: id,
            AzureAdObjectId: null,
            Email: email,
            DisplayName: null,
            InvitedAtUtc: DateTime.UtcNow,
            LastLoginAtUtc: null,
            CreatedAtUtc: DateTime.UtcNow,
            DeletedAtUtc: null));
        return Task.FromResult(id);
    }

    public Task UpdateObjectIdAndLoginAsync(int userId, string objectId, string? displayName, CancellationToken ct)
    {
        UpdateObjectIdAndLoginCalls.Add((userId, objectId, displayName));

        var index = _users.FindIndex(u => u.Id == userId);
        if (index >= 0)
        {
            var existing = _users[index];
            _users[index] = existing with
            {
                AzureAdObjectId = objectId,
                DisplayName = displayName ?? existing.DisplayName,
                LastLoginAtUtc = DateTime.UtcNow,
            };
        }

        return Task.CompletedTask;
    }

    /// <summary>Seeds an already-resolved user (useful for middleware oid-lookup tests).</summary>
    public User SeedExisting(
        string email,
        string? azureAdObjectId = null,
        string? displayName = null)
    {
        var id = _nextId++;
        var user = new User(
            Id: id,
            AzureAdObjectId: azureAdObjectId,
            Email: email,
            DisplayName: displayName,
            InvitedAtUtc: DateTime.UtcNow,
            LastLoginAtUtc: null,
            CreatedAtUtc: DateTime.UtcNow,
            DeletedAtUtc: null);
        _users.Add(user);
        return user;
    }
}
