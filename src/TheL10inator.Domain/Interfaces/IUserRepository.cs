using TheL10inator.Domain.Models;

namespace TheL10inator.Domain.Interfaces;

/// <summary>
/// Reads and writes the <c>Users</c> table. The same row represents both the invite and the
/// resolved identity — the <c>AzureAdObjectId</c> column is backfilled on first login.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Looks up a non-deleted user by their Entra object id. Returns <c>null</c> when the
    /// caller has authenticated successfully but never been invited.
    /// </summary>
    Task<User?> GetByAzureAdObjectIdAsync(string objectId, CancellationToken ct);

    /// <summary>
    /// Looks up a non-deleted user by email. Used during first-login bridging when the
    /// object id is not yet populated.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);

    /// <summary>
    /// Inserts a row representing an invited user with <c>AzureAdObjectId = NULL</c> and
    /// <c>InvitedAtUtc = SYSUTCDATETIME()</c>. Returns the new identity value.
    /// </summary>
    Task<int> InsertInvitedAsync(string email, CancellationToken ct);

    /// <summary>
    /// Bridges an invited user to a real principal on first login: populates
    /// <c>AzureAdObjectId</c>, updates <c>LastLoginAtUtc</c>, and applies the display
    /// name from the token if supplied.
    /// </summary>
    Task UpdateObjectIdAndLoginAsync(int userId, string objectId, string? displayName, CancellationToken ct);
}
