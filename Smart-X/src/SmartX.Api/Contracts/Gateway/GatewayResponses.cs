namespace SmartX.Api.Contracts.Gateway;

/// <summary>
/// Contains the current health state of the Smart-X gateway.
/// </summary>
public sealed record GatewayHealthResponse(
    string Status,
    string Service,
    string Version,
    DateTimeOffset CheckedAtUtc,
    int RegisteredSensors,
    TelemetrySummaryResponse Telemetry);

/// <summary>
/// Contains dashboard-ready telemetry totals.
/// </summary>
public sealed record TelemetrySummaryResponse(
    int Total,
    int Accepted,
    int Flagged,
    int Rejected,
    int FloatingPoint,
    int Integer,
    int Boolean);

/// <summary>
/// Contains the information displayed by the startup interface.
/// </summary>
public sealed record GatewayStartupResponse(
    string ApplicationName,
    string Tagline,
    string Environment,
    IReadOnlyList<ArchitecturalPillarResponse> Pillars);

/// <summary>
/// Describes one architectural pillar on the gateway landing page.
/// </summary>
public sealed record ArchitecturalPillarResponse(
    int Order,
    string Key,
    string Title,
    string Description,
    bool IsEnabled,
    string Availability,
    string? Route);