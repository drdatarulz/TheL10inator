using TheL10inator.Domain.Models;

namespace TheL10inator.Domain.Interfaces;

/// <summary>
/// Reads and writes the <c>TeamMembers</c> table — the join row binding a user to a team
/// along with their role.
/// </summary>
public interface ITeamMemberRepository
{
    /// <summary>
    /// Returns <c>true</c> if any non-deleted team member row has <c>Role = 'Admin'</c>.
    /// Drives the idempotent branch of the first-admin seed.
    /// </summary>
    Task<bool> HasAnyAdminAsync(CancellationToken ct);

    /// <summary>
    /// Returns the single non-deleted team membership for the supplied user id, if any.
    /// v1 assumes each user belongs to at most one team.
    /// </summary>
    Task<TeamMember?> GetByUserIdAsync(int userId, CancellationToken ct);

    /// <summary>
    /// Inserts a new team membership row and returns its identity value.
    /// </summary>
    Task<int> InsertAsync(int teamId, int userId, TeamRole role, CancellationToken ct);
}
