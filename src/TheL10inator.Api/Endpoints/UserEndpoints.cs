using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using TheL10inator.Api.Models.Responses;
using TheL10inator.Domain.Interfaces;
using TheL10inator.Domain.Models;

namespace TheL10inator.Api.Endpoints;

/// <summary>
/// User-facing endpoints. v1 exposes only <c>GET /api/users/me</c>; later milestones add the
/// invite flow and admin-only user management here.
/// </summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapGet("/api/users/me", (ICurrentUserAccessor users) =>
        {
            var current = users.GetRequired();
            var response = new UserMeResponse
            {
                UserId = current.UserId,
                ObjectId = current.AzureAdObjectId,
                Email = current.Email,
                DisplayName = current.DisplayName,
                Role = current.Role.ToString(),
                TeamId = current.TeamId,
                TeamName = current.TeamName,
            };
            return Results.Ok(response);
        })
            .WithName("GetCurrentUser")
            .Produces<UserMeResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization();
    }
}
