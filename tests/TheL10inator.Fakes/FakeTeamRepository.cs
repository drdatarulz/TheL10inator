using TheL10inator.Domain.Interfaces;
using TheL10inator.Domain.Models;

namespace TheL10inator.Fakes;

/// <summary>
/// In-memory implementation of <see cref="ITeamRepository"/> for unit tests.
/// Mirrors the Dapper repository's singleton-read semantics without touching SQL.
/// </summary>
public sealed class FakeTeamRepository : ITeamRepository
{
    private readonly List<Team> _teams = new();
    private int _nextId = 1;

    public IReadOnlyList<Team> Teams => _teams;

    public Task<Team?> GetSingletonAsync(CancellationToken ct)
    {
        var team = _teams.FirstOrDefault(t => t.DeletedAtUtc is null);
        return Task.FromResult(team);
    }

    public Task<int> InsertAsync(string name, CancellationToken ct)
    {
        var id = _nextId++;
        _teams.Add(new Team(id, name, DateTime.UtcNow, null));
        return Task.FromResult(id);
    }
}
