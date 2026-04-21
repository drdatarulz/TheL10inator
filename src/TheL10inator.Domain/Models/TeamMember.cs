namespace TheL10inator.Domain.Models;

/// <summary>
/// Join row linking a <see cref="User"/> to a <see cref="Team"/> along with their role on that team.
/// </summary>
public sealed record TeamMember(
    int Id,
    int TeamId,
    int UserId,
    TeamRole Role,
    DateTime JoinedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? DeletedAtUtc);
