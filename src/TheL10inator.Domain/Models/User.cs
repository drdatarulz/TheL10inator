namespace TheL10inator.Domain.Models;

/// <summary>
/// A user known to the application. Acts simultaneously as the invite record
/// (<see cref="InvitedAtUtc"/>) and the identity record (<see cref="AzureAdObjectId"/>,
/// <see cref="LastLoginAtUtc"/>). <see cref="AzureAdObjectId"/> remains null until the
/// first successful Entra login bridges the invited row to a real principal.
/// </summary>
public sealed record User(
    int Id,
    string? AzureAdObjectId,
    string Email,
    string? DisplayName,
    DateTime InvitedAtUtc,
    DateTime? LastLoginAtUtc,
    DateTime CreatedAtUtc,
    DateTime? DeletedAtUtc);
