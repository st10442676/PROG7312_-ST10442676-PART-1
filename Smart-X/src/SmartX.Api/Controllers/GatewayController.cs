using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Contracts.Gateway;
using SmartX.Api.Services;
using SmartX.Domain.Repositories;

namespace SmartX.Api.Controllers;

/// <summary>
/// Exposes gateway startup and health information to the client.
/// </summary>
[ApiController]
[Route("api/gateway")]
public sealed class GatewayController : ControllerBase
{
    private readonly ISensorProfileRepository _sensorRepository;
    private readonly TelemetryHistoryStore _telemetryHistory;

    public GatewayController(
        ISensorProfileRepository sensorRepository,
        TelemetryHistoryStore telemetryHistory)
    {
        _sensorRepository = sensorRepository;
        _telemetryHistory = telemetryHistory;
    }

    /// <summary>
    /// Returns the current operational state of the Smart-X API.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType<GatewayHealthResponse>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<GatewayHealthResponse>> GetHealthAsync(
        CancellationToken cancellationToken)
    {
        int registeredSensorCount =
            await _sensorRepository.CountAsync(
                cancellationToken);

        TelemetryHistorySummary telemetry =
            _telemetryHistory.GetSummary();

        GatewayHealthResponse response = new(
            Status: "Healthy",
            Service: "Smart-X Data Ingestion Gateway",
            Version: "1.0.0",
            CheckedAtUtc: DateTimeOffset.UtcNow,
            RegisteredSensors: registeredSensorCount,
            Telemetry: new TelemetrySummaryResponse(
                telemetry.Total,
                telemetry.Accepted,
                telemetry.Flagged,
                telemetry.Rejected,
                telemetry.FloatingPoint,
                telemetry.Integer,
                telemetry.Boolean));

        return Ok(response);
    }

    /// <summary>
    /// Returns the architectural pillars displayed on the landing page.
    /// </summary>
    [HttpGet("startup")]
    [ProducesResponseType<GatewayStartupResponse>(
        StatusCodes.Status200OK)]
    public ActionResult<GatewayStartupResponse> GetStartup()
    {
        List<ArchitecturalPillarResponse> pillars =
        [
            new ArchitecturalPillarResponse(
                Order: 1,
                Key: "sensor-ingestion",
                Title: "Sensor Data Ingestion and Telemetry",
                Description:
                    "Register sensors, submit strongly typed telemetry, " +
                    "attach evidence and investigate unusual readings.",
                IsEnabled: true,
                Availability: "Available in Part 1",
                Route: "/telemetry"),

            new ArchitecturalPillarResponse(
                Order: 2,
                Key: "command-stream",
                Title: "Real-Time Command Stream and History",
                Description:
                    "Prioritise device commands and review historical " +
                    "execution activity.",
                IsEnabled: false,
                Availability: "Coming in Part 2",
                Route: null),

            new ArchitecturalPillarResponse(
                Order: 3,
                Key: "mesh-routing",
                Title: "Network Topology and Mesh Routing",
                Description:
                    "Visualise mesh relationships and trace sensor " +
                    "routing paths to the gateway.",
                IsEnabled: false,
                Availability: "Coming in the final PoE",
                Route: null)
        ];

        GatewayStartupResponse response = new(
            ApplicationName: "Smart-X IoT Gateway",
            Tagline: "Observe. Detect. Resolve.",
            Environment:
                HttpContext.RequestServices
                    .GetRequiredService<IHostEnvironment>()
                    .EnvironmentName,
            Pillars: pillars.AsReadOnly());

        return Ok(response);
    }
}