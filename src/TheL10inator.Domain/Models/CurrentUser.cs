namespace TheL10inator.Domain.Models;

/// <summary>
/// Per-request projection of the authenticated caller, resolved by
/// <c>UserResolutionMiddleware</c> and stashed on <c>HttpContext.Items["CurrentUser"]</c>.
/// Endpoints consume this through <c>ICurrentUserAccessor</c>; the Admin authorization
/// policy evaluates its <see cref="Role"/>.
/// </summary>
public sealed record CurrentUser(
    int UserId,
    string AzureAdObjectId,
    string Email,
    string? DisplayName,
    int TeamId,
    string TeamName,
    TeamRole Role);
