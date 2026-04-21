using TheL10inator.Domain.Interfaces;
using TheL10inator.Domain.Models;

namespace TheL10inator.Fakes;

/// <summary>
/// In-memory implementation of <see cref="ITeamMemberRepository"/> for unit tests.
/// </summary>
public sealed class FakeTeamMemberRepository : ITeamMemberRepository
{
    private readonly List<TeamMember> _members = new();
    private int _nextId = 1;

    public IReadOnlyList<TeamMember> Members => _members;

    public Task<bool> HasAnyAdminAsync(CancellationToken ct)
    {
        var any = _members.Any(m => m.DeletedAtUtc is null && m.Role == TeamRole.Admin);
        return Task.FromResult(any);
    }

    public Task<TeamMember?> GetByUserIdAsync(int userId, CancellationToken ct)
    {
        var member = _members.FirstOrDefault(m => m.DeletedAtUtc is null && m.UserId == userId);
        return Task.FromResult(member);
    }

    public Task<int> InsertAsync(int teamId, int userId, TeamRole role, CancellationToken ct)
    {
        var id = _nextId++;
        _members.Add(new TeamMember(id, teamId, userId, role, DateTime.UtcNow, DateTime.UtcNow, null));
        return Task.FromResult(id);
    }
}
