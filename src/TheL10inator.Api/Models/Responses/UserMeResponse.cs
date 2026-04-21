using System.Text.Json.Serialization;

namespace TheL10inator.Api.Models.Responses;

/// <summary>
/// Payload returned by <c>GET /api/users/me</c>: the canonical projection of the
/// currently authenticated caller. The Angular shell renders the user's display name and
/// team name directly from these fields.
/// </summary>
public sealed class UserMeResponse
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    [JsonPropertyName("objectId")]
    public string ObjectId { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("teamId")]
    public int TeamId { get; set; }

    [JsonPropertyName("teamName")]
    public string TeamName { get; set; } = string.Empty;
}
