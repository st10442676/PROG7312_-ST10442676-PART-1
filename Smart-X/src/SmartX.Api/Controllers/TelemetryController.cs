using System.Numerics;
using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Contracts.Telemetry;
using SmartX.Api.Services;
using SmartX.Domain.Entities;
using SmartX.Domain.Enums;
using SmartX.Domain.Repositories;
using SmartX.Domain.Telemetry;

namespace SmartX.Api.Controllers;

/// <summary>
/// Receives and retrieves strongly typed Smart-X telemetry.
/// </summary>
[ApiController]
[Route("api/sensors/{sensorId:guid}/telemetry")]
public sealed class TelemetryController : ControllerBase
{
    private readonly ISensorProfileRepository _sensorRepository;
    private readonly TelemetryHistoryStore _telemetryHistory;

    public TelemetryController(
        ISensorProfileRepository sensorRepository,
        TelemetryHistoryStore telemetryHistory)
    {
        _sensorRepository = sensorRepository;
        _telemetryHistory = telemetryHistory;
    }

    /// <summary>
    /// Ingests a floating-point sensor reading.
    /// </summary>
    [HttpPost("float")]
    [ProducesResponseType<TelemetryIngestionResponse<float>>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<TelemetryIngestionResponse<float>>>
        IngestFloatingPointAsync(
            Guid sensorId,
            [FromBody] IngestTelemetryRequest<float> request,
            CancellationToken cancellationToken)
    {
        return IngestAsync(
            sensorId,
            request,
            TelemetryDataType.FloatingPoint,
            static (sensor, value) =>
                EvaluateNumericReading(sensor, value),
            (packet, state, message) =>
                _telemetryHistory.Record(
                    packet,
                    state,
                    message),
            cancellationToken);
    }

    /// <summary>
    /// Ingests an integer sensor reading.
    /// </summary>
    [HttpPost("integer")]
    [ProducesResponseType<TelemetryIngestionResponse<int>>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<TelemetryIngestionResponse<int>>>
        IngestIntegerAsync(
            Guid sensorId,
            [FromBody] IngestTelemetryRequest<int> request,
            CancellationToken cancellationToken)
    {
        return IngestAsync(
            sensorId,
            request,
            TelemetryDataType.Integer,
            static (sensor, value) =>
                EvaluateNumericReading(sensor, value),
            (packet, state, message) =>
                _telemetryHistory.Record(
                    packet,
                    state,
                    message),
            cancellationToken);
    }

    /// <summary>
    /// Ingests a Boolean actuator reading.
    /// </summary>
    [HttpPost("boolean")]
    [ProducesResponseType<TelemetryIngestionResponse<bool>>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<TelemetryIngestionResponse<bool>>>
        IngestBooleanAsync(
            Guid sensorId,
            [FromBody] IngestTelemetryRequest<bool> request,
            CancellationToken cancellationToken)
    {
        return IngestAsync(
            sensorId,
            request,
            TelemetryDataType.Boolean,
            static (_, _) =>
                TelemetryEvaluation.Accepted(),
            (packet, state, message) =>
                _telemetryHistory.Record(
                    packet,
                    state,
                    message),
            cancellationToken);
    }

    /// <summary>
    /// Returns all retained telemetry for a sensor while keeping
    /// floating-point, integer and Boolean values separate.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<SensorTelemetryHistoryResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SensorTelemetryHistoryResponse>>
        GetHistoryAsync(
            Guid sensorId,
            CancellationToken cancellationToken)
    {
        SensorProfile? sensor =
            await _sensorRepository.GetByIdAsync(
                sensorId,
                cancellationToken);

        if (sensor is null)
        {
            return NotFound(
                CreateSensorNotFoundProblem(sensorId));
        }

        List<TelemetryIngestionResponse<float>>
            floatingPointReadings =
                MapHistory(
                    _telemetryHistory
                        .GetFloatingPointHistory(),
                    sensorId);

        List<TelemetryIngestionResponse<int>>
            integerReadings =
                MapHistory(
                    _telemetryHistory
                        .GetIntegerHistory(),
                    sensorId);

        List<TelemetryIngestionResponse<bool>>
            booleanReadings =
                MapHistory(
                    _telemetryHistory
                        .GetBooleanHistory(),
                    sensorId);

        SensorTelemetryHistoryResponse response = new(
            sensorId,
            floatingPointReadings.AsReadOnly(),
            integerReadings.AsReadOnly(),
            booleanReadings.AsReadOnly());

        return Ok(response);
    }

    private async Task<ActionResult<TelemetryIngestionResponse<T>>>
        IngestAsync<T>(
            Guid sensorId,
            IngestTelemetryRequest<T> request,
            TelemetryDataType requiredDataType,
            Func<SensorProfile, T, TelemetryEvaluation>
                evaluateReading,
            Action<
                TelemetryPacket<T>,
                TelemetryIngestionState,
                string?> storePacket,
            CancellationToken cancellationToken)
        where T : struct
    {
        SensorProfile? sensor =
            await _sensorRepository.GetByIdAsync(
                sensorId,
                cancellationToken);

        if (sensor is null)
        {
            return NotFound(
                CreateSensorNotFoundProblem(sensorId));
        }

        if (sensor.DataType != requiredDataType)
        {
            return Conflict(
                new ProblemDetails
                {
                    Title = "Telemetry type mismatch",
                    Detail =
                        $"Sensor '{sensor.DisplayName}' expects " +
                        $"{sensor.DataType} telemetry, but the request " +
                        $"used the {requiredDataType} endpoint.",
                    Status = StatusCodes.Status409Conflict,
                    Instance = HttpContext.Request.Path
                });
        }

        if (request.CapturedAtUtc == default)
        {
            ModelState.AddModelError(
                nameof(request.CapturedAtUtc),
                "A valid capturedAtUtc timestamp is required.");

            return ValidationProblem(ModelState);
        }

        try
        {
            DateTimeOffset receivedAtUtc =
                DateTimeOffset.UtcNow;

            TelemetryEvaluation evaluation =
                evaluateReading(
                    sensor,
                    request.Value);

            TelemetryPacket<T> packet =
                TelemetryPacket<T>.Create(
                    sensorId,
                    request.SequenceNumber,
                    request.Value,
                    sensor.Unit,
                    request.CapturedAtUtc,
                    receivedAtUtc,
                    evaluation.HealthState);

            storePacket(
                packet,
                evaluation.IngestionState,
                evaluation.Message);

            sensor.RecordTelemetry(receivedAtUtc);

            TelemetryIngestionResponse<T> response =
                TelemetryIngestionResponse<T>.FromDomain(
                    packet,
                    evaluation.IngestionState,
                    evaluation.Message);

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(
                "telemetry",
                exception.Message);

            return ValidationProblem(ModelState);
        }
        catch (NotSupportedException exception)
        {
            ModelState.AddModelError(
                "telemetryType",
                exception.Message);

            return ValidationProblem(ModelState);
        }
    }

    private static TelemetryEvaluation EvaluateNumericReading<T>(
        SensorProfile sensor,
        T value)
        where T : struct, INumber<T>
    {
        double numericValue =
            double.CreateChecked(value);

        bool belowExpectedMinimum =
            sensor.ExpectedMinimum.HasValue &&
            numericValue < sensor.ExpectedMinimum.Value;

        bool aboveExpectedMaximum =
            sensor.ExpectedMaximum.HasValue &&
            numericValue > sensor.ExpectedMaximum.Value;

        if (belowExpectedMinimum ||
            aboveExpectedMaximum)
        {
            string message =
                $"Reading {numericValue} {sensor.Unit} is outside " +
                $"the expected range of {sensor.ExpectedMinimum} to " +
                $"{sensor.ExpectedMaximum} {sensor.Unit}.";

            return TelemetryEvaluation.Flagged(
                TelemetryHealthState.OutOfRange,
                message);
        }

        return TelemetryEvaluation.Accepted();
    }

    private static List<TelemetryIngestionResponse<T>>
        MapHistory<T>(
            IReadOnlyList<
                TelemetryIngestionRecord<TelemetryPacket<T>>> history,
            Guid sensorId)
        where T : struct
    {
        return history
            .Where(record =>
                record.Item.SensorId == sensorId)
            .Select(record =>
                TelemetryIngestionResponse<T>.FromDomain(
                    record.Item,
                    record.State,
                    record.Message))
            .ToList();
    }

    private ProblemDetails CreateSensorNotFoundProblem(
        Guid sensorId)
    {
        return new ProblemDetails
        {
            Title = "Sensor profile not found",
            Detail =
                $"No sensor profile exists with identifier '{sensorId}'.",
            Status = StatusCodes.Status404NotFound,
            Instance = HttpContext.Request.Path
        };
    }

    private sealed record TelemetryEvaluation(
        TelemetryHealthState HealthState,
        TelemetryIngestionState IngestionState,
        string? Message)
    {
        public static TelemetryEvaluation Accepted()
        {
            return new TelemetryEvaluation(
                TelemetryHealthState.Normal,
                TelemetryIngestionState.Accepted,
                Message: null);
        }

        public static TelemetryEvaluation Flagged(
            TelemetryHealthState healthState,
            string message)
        {
            return new TelemetryEvaluation(
                healthState,
                TelemetryIngestionState.Flagged,
                message);
        }
    }
}