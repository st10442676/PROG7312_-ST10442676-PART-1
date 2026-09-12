using System.Text.Json.Serialization;

namespace SmartX.Client.Models;

/// <summary>
/// Represents the health information returned by the Smart-X API gateway.
/// </summary>
public sealed class GatewayHealthResponse
{
    [JsonPropertyName("application")]
    public string Application { get; init; } = "Smart-X IoT Gateway";

    [JsonPropertyName("status")]
    public string Status { get; init; } = "Online";

    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; init; } = "1.0";

    [JsonPropertyName("timestampUtc")]
    public DateTimeOffset? TimestampUtc { get; init; }

    [JsonPropertyName("healthEndpoint")]
    public string? HealthEndpoint { get; init; }

    [JsonPropertyName("startupEndpoint")]
    public string? StartupEndpoint { get; init; }
}