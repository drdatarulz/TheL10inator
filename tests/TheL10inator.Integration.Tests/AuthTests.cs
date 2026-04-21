using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Shouldly;
using TheL10inator.Api.Authentication;
using TheL10inator.Integration.Tests.Fixtures;

namespace TheL10inator.Integration.Tests;

[Collection(nameof(IntegrationCollection))]
public class AuthTests
{
    private readonly IntegrationFixture _fixture;

    public AuthTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Seeded_admin_returns_200_from_users_me()
    {
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add(DevBypassAuthHandler.EmailHeaderName, IntegrationFixture.SeededAdminEmail);

        var response = await client.GetAsync("/api/users/me");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<UserMeResponseDto>();
        body.ShouldNotBeNull();
        body!.Email.ShouldBe(IntegrationFixture.SeededAdminEmail);
        body.Role.ShouldBe("Admin");
        body.TeamName.ShouldBe("Leadership");
        body.UserId.ShouldBeGreaterThan(0);
        body.TeamId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Unknown_email_returns_403_from_users_me()
    {
        using var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add(DevBypassAuthHandler.EmailHeaderName, "stranger@example.com");

        var response = await client.GetAsync("/api/users/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_request_returns_401_from_users_me()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Health_live_returns_200_without_auth()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_ready_returns_200_without_auth()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private sealed class UserMeResponseDto
    {
        [JsonPropertyName("userId")] public int UserId { get; set; }
        [JsonPropertyName("objectId")] public string ObjectId { get; set; } = string.Empty;
        [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
        [JsonPropertyName("displayName")] public string? DisplayName { get; set; }
        [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;
        [JsonPropertyName("teamId")] public int TeamId { get; set; }
        [JsonPropertyName("teamName")] public string TeamName { get; set; } = string.Empty;
    }
}
