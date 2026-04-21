using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using TheL10inator.Domain.Interfaces;
using TheL10inator.Domain.Models;

namespace TheL10inator.Api.Authentication;

/// <summary>
/// Resolves the authenticated caller to a <see cref="CurrentUser"/> and stashes it on
/// <c>HttpContext.Items["CurrentUser"]</c> for downstream endpoints and authorization
/// policies. Runs after <c>UseAuthentication()</c> / <c>UseAuthorization()</c> but before
/// any endpoint executes. Anonymous requests (for example <c>/health/live</c>) pass through
/// untouched; authenticated requests whose identity cannot be matched to an invited
/// <c>Users</c> row short-circuit with a 403 body-less response.
/// </summary>
public sealed class UserResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserResolutionMiddleware> _logger;

    public UserResolutionMiddleware(RequestDelegate next, ILogger<UserResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IUserRepository userRepository,
        ITeamMemberRepository teamMemberRepository,
        ITeamRepository teamRepository)
    {
        var principal = context.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var ct = context.RequestAborted;
        var oid = principal.FindFirst("oid")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = principal.FindFirst("preferred_username")?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value;
        var displayName = principal.FindFirst("name")?.Value;

        if (string.IsNullOrWhiteSpace(oid) && string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Authenticated request carried neither an oid nor an email claim; denying access.");
            await WriteForbidAsync(context).ConfigureAwait(false);
            return;
        }

        User? user = null;
        if (!string.IsNullOrWhiteSpace(oid))
        {
            user = await userRepository.GetByAzureAdObjectIdAsync(oid, ct).ConfigureAwait(false);
        }

        if (user is null && !string.IsNullOrWhiteSpace(email))
        {
            var invited = await userRepository.GetByEmailAsync(email, ct).ConfigureAwait(false);
            if (invited is not null && !string.IsNullOrWhiteSpace(oid))
            {
                await userRepository.UpdateObjectIdAndLoginAsync(invited.Id, oid!, displayName, ct)
                    .ConfigureAwait(false);
                user = invited with
                {
                    AzureAdObjectId = oid,
                    DisplayName = displayName ?? invited.DisplayName,
                    LastLoginAtUtc = DateTime.UtcNow,
                };
            }
            else
            {
                user = invited;
            }
        }

        if (user is null)
        {
            _logger.LogWarning(
                "Authenticated caller could not be resolved to a Users row. oid={HasOid} email={HasEmail}",
                !string.IsNullOrWhiteSpace(oid), !string.IsNullOrWhiteSpace(email));
            await WriteForbidAsync(context).ConfigureAwait(false);
            return;
        }

        var membership = await teamMemberRepository.GetByUserIdAsync(user.Id, ct).ConfigureAwait(false);
        if (membership is null)
        {
            _logger.LogWarning("Resolved user {UserId} has no team membership; denying access.", user.Id);
            await WriteForbidAsync(context).ConfigureAwait(false);
            return;
        }

        var team = await teamRepository.GetSingletonAsync(ct).ConfigureAwait(false);
        if (team is null || team.Id != membership.TeamId)
        {
            _logger.LogWarning("Team {TeamId} referenced by membership {MembershipId} was not found.",
                membership.TeamId, membership.Id);
            await WriteForbidAsync(context).ConfigureAwait(false);
            return;
        }

        var currentUser = new CurrentUser(
            UserId: user.Id,
            AzureAdObjectId: user.AzureAdObjectId ?? oid ?? string.Empty,
            Email: user.Email,
            DisplayName: user.DisplayName ?? displayName,
            TeamId: team.Id,
            TeamName: team.Name,
            Role: membership.Role);

        context.Items[CurrentUserAccessor.CurrentUserItemKey] = currentUser;

        using (LogContext.PushProperty("UserId", currentUser.UserId))
        using (LogContext.PushProperty("TeamId", currentUser.TeamId))
        {
            await _next(context).ConfigureAwait(false);
        }
    }

    private static Task WriteForbidAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
