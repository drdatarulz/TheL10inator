using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TheL10inator.Api.Authentication;
using TheL10inator.Domain.Models;
using TheL10inator.Fakes;

namespace TheL10inator.Api.Tests.Authentication;

public class UserResolutionMiddlewareTests
{
    [Fact]
    public async Task Resolves_by_oid_when_user_exists()
    {
        var users = new FakeUserRepository();
        var teams = new FakeTeamRepository();
        var members = new FakeTeamMemberRepository();

        var teamId = await teams.InsertAsync("Leadership", CancellationToken.None);
        var user = users.SeedExisting(
            email: "kevin@example.com",
            azureAdObjectId: "oid-123",
            displayName: "Kevin");
        await members.InsertAsync(teamId, user.Id, TeamRole.Admin, CancellationToken.None);

        var context = BuildHttpContext(oid: "oid-123", email: "kevin@example.com", displayName: "Kevin");
        var middleware = new UserResolutionMiddleware(_ => Task.CompletedTask, NullLogger<UserResolutionMiddleware>.Instance);

        await middleware.InvokeAsync(context, users, members, teams);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var current = context.Items[CurrentUserAccessor.CurrentUserItemKey].ShouldBeOfType<CurrentUser>();
        current.UserId.ShouldBe(user.Id);
        current.Email.ShouldBe("kevin@example.com");
        current.Role.ShouldBe(TeamRole.Admin);
        current.TeamName.ShouldBe("Leadership");
        users.UpdateObjectIdAndLoginCalls.ShouldBeEmpty();
    }

    [Fact]
    public async Task Bridges_by_email_and_updates_oid_on_first_login()
    {
        var users = new FakeUserRepository();
        var teams = new FakeTeamRepository();
        var members = new FakeTeamMemberRepository();

        var teamId = await teams.InsertAsync("Leadership", CancellationToken.None);
        // Invited row with no oid yet (mirrors the seeded admin before first login).
        var invitedId = await users.InsertInvitedAsync("kevin@example.com", CancellationToken.None);
        await members.InsertAsync(teamId, invitedId, TeamRole.Admin, CancellationToken.None);

        var context = BuildHttpContext(oid: "oid-new", email: "kevin@example.com", displayName: "Kevin P");
        var middleware = new UserResolutionMiddleware(_ => Task.CompletedTask, NullLogger<UserResolutionMiddleware>.Instance);

        await middleware.InvokeAsync(context, users, members, teams);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var current = context.Items[CurrentUserAccessor.CurrentUserItemKey].ShouldBeOfType<CurrentUser>();
        current.UserId.ShouldBe(invitedId);
        current.AzureAdObjectId.ShouldBe("oid-new");
        current.DisplayName.ShouldBe("Kevin P");

        users.UpdateObjectIdAndLoginCalls.Count.ShouldBe(1);
        users.UpdateObjectIdAndLoginCalls[0].UserId.ShouldBe(invitedId);
        users.UpdateObjectIdAndLoginCalls[0].ObjectId.ShouldBe("oid-new");
        users.UpdateObjectIdAndLoginCalls[0].DisplayName.ShouldBe("Kevin P");
    }

    [Fact]
    public async Task Returns_403_when_oid_and_email_both_miss()
    {
        var users = new FakeUserRepository();
        var teams = new FakeTeamRepository();
        var members = new FakeTeamMemberRepository();

        var context = BuildHttpContext(oid: "oid-unknown", email: "stranger@example.com", displayName: "Stranger");
        var nextCalled = false;
        var middleware = new UserResolutionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, NullLogger<UserResolutionMiddleware>.Instance);

        await middleware.InvokeAsync(context, users, members, teams);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        nextCalled.ShouldBeFalse();
        context.Items.ContainsKey(CurrentUserAccessor.CurrentUserItemKey).ShouldBeFalse();
    }

    [Fact]
    public async Task Passes_through_anonymous_requests()
    {
        var users = new FakeUserRepository();
        var teams = new FakeTeamRepository();
        var members = new FakeTeamMemberRepository();

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()) // Not authenticated.
        };

        var nextCalled = false;
        var middleware = new UserResolutionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, NullLogger<UserResolutionMiddleware>.Instance);

        await middleware.InvokeAsync(context, users, members, teams);

        nextCalled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    private static HttpContext BuildHttpContext(string? oid, string? email, string? displayName)
    {
        var claims = new List<Claim>();
        if (oid is not null) claims.Add(new Claim("oid", oid));
        if (email is not null) claims.Add(new Claim("preferred_username", email));
        if (displayName is not null) claims.Add(new Claim("name", displayName));

        var identity = new ClaimsIdentity(claims, "Test");
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
        };
        return context;
    }
}
