using TheL10inator.Domain.Models;

namespace TheL10inator.Domain.Interfaces;

/// <summary>
/// Reads and writes the <c>Teams</c> table. v1 exposes only the singleton Leadership team.
/// </summary>
public interface ITeamRepository
{
    /// <summary>
    /// Returns the single non-deleted team, if any. The admin-seed flow inserts one row
    /// on first start; every subsequent call should return the same row.
    /// </summary>
    Task<Team?> GetSingletonAsync(CancellationToken ct);

    /// <summary>
    /// Inserts a new team with the supplied name and returns its identity value.
    /// </summary>
    Task<int> InsertAsync(string name, CancellationToken ct);
}
