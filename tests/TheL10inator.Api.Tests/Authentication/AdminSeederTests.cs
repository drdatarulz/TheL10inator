using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TheL10inator.Domain.Models;
using TheL10inator.Fakes;
using TheL10inator.Infrastructure.Startup;

namespace TheL10inator.Api.Tests.Authentication;

public class AdminSeederTests
{
    [Fact]
    public async Task Seeds_admin_when_no_admin_exists()
    {
        var teams = new FakeTeamRepository();
        var users = new FakeUserRepository();
        var members = new FakeTeamMemberRepository();
        var seeder = new AdminSeeder(teams, users, members, "kevin@example.com", NullLogger<AdminSeeder>.Instance);

        await seeder.SeedIfMissingAsync(CancellationToken.None);

        teams.Teams.Count.ShouldBe(1);
        teams.Teams[0].Name.ShouldBe(AdminSeeder.LeadershipTeamName);

        users.Users.Count.ShouldBe(1);
        users.Users[0].Email.ShouldBe("kevin@example.com");
        users.Users[0].AzureAdObjectId.ShouldBeNull();
        users.Users[0].InvitedAtUtc.ShouldNotBe(default);

        members.Members.Count.ShouldBe(1);
        members.Members[0].Role.ShouldBe(TeamRole.Admin);
        members.Members[0].TeamId.ShouldBe(teams.Teams[0].Id);
        members.Members[0].UserId.ShouldBe(users.Users[0].Id);
    }

    [Fact]
    public async Task Is_idempotent_when_admin_exists()
    {
        var teams = new FakeTeamRepository();
        var users = new FakeUserRepository();
        var members = new FakeTeamMemberRepository();

        var existingTeamId = await teams.InsertAsync("ExistingTeam", CancellationToken.None);
        var existingUserId = await users.InsertInvitedAsync("existing@example.com", CancellationToken.None);
        await members.InsertAsync(existingTeamId, existingUserId, TeamRole.Admin, CancellationToken.None);

        var seeder = new AdminSeeder(teams, users, members, "newadmin@example.com", NullLogger<AdminSeeder>.Instance);

        await seeder.SeedIfMissingAsync(CancellationToken.None);

        teams.Teams.Count.ShouldBe(1);
        users.Users.Count.ShouldBe(1);
        members.Members.Count.ShouldBe(1);
        users.Users[0].Email.ShouldBe("existing@example.com");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Throws_when_FirstAdminEmail_missing(string? email)
    {
        var teams = new FakeTeamRepository();
        var users = new FakeUserRepository();
        var members = new FakeTeamMemberRepository();
        var seeder = new AdminSeeder(teams, users, members, email, NullLogger<AdminSeeder>.Instance);

        await Should.ThrowAsync<InvalidOperationException>(() => seeder.SeedIfMissingAsync(CancellationToken.None));
        teams.Teams.Count.ShouldBe(0);
        users.Users.Count.ShouldBe(0);
        members.Members.Count.ShouldBe(0);
    }
}
