namespace TheL10inator.Domain.Models;

/// <summary>
/// The two roles a user can hold on a team. Mirrors the <c>CHECK</c> constraint on
/// <c>dbo.TeamMembers.Role</c> — stored as the enum member name.
/// </summary>
public enum TeamRole
{
    Member = 0,
    Admin = 1,
}
