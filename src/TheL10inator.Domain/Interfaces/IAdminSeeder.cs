namespace TheL10inator.Domain.Interfaces;

/// <summary>
/// Seeds the first Leadership team and its admin user from configuration when no admin
/// team member exists yet. Idempotent — safe to invoke on every startup.
/// </summary>
public interface IAdminSeeder
{
    /// <summary>
    /// Inspects the database for any existing Admin team member; when none exists,
    /// creates one team, one user (with <c>InvitedAtUtc</c> set), and one team member
    /// row in a single transaction. Throws <see cref="InvalidOperationException"/>
    /// when <c>Administration:FirstAdminEmail</c> is not configured.
    /// </summary>
    Task SeedIfMissingAsync(CancellationToken ct);
}
