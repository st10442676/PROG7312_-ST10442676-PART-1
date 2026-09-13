using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Contracts.Sensors;
using SmartX.Domain.Entities;
using SmartX.Domain.Repositories;

namespace SmartX.Api.Controllers;

/// <summary>
/// Manages Smart-X sensor registration and profile retrieval.
/// </summary>
[ApiController]
[Route("api/sensors")]
public sealed class SensorsController : ControllerBase
{
    private readonly ISensorProfileRepository _sensorRepository;

    public SensorsController(
        ISensorProfileRepository sensorRepository)
    {
        ArgumentNullException.ThrowIfNull(sensorRepository);

        _sensorRepository = sensorRepository;
    }

    /// <summary>
    /// Returns all registered sensors.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<
        IReadOnlyList<SensorProfileResponse>>(
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<IReadOnlyList<SensorProfileResponse>>>
        GetAllAsync(
            CancellationToken cancellationToken)
    {
        IReadOnlyList<SensorProfile> sensors =
            await _sensorRepository.GetAllAsync(
                cancellationToken);

        List<SensorProfileResponse> response =
            sensors
                .Select(SensorProfileResponse.FromDomain)
                .ToList();

        return Ok(response);
    }

    /// <summary>
    /// Returns one sensor using its internal profile identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<SensorProfileResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SensorProfileResponse>>
        GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken)
    {
        SensorProfile? sensor =
            await _sensorRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (sensor is null)
        {
            return NotFound(
                CreateSensorNotFoundProblem(id));
        }

        return Ok(
            SensorProfileResponse.FromDomain(sensor));
    }

    /// <summary>
    /// Registers and validates a new Smart-X sensor.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<SensorProfileResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SensorProfileResponse>>
        RegisterAsync(
            [FromBody] RegisterSensorRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            SensorProfile sensor =
                SensorProfile.Create(
                    request.DeviceIdentifier,
                    request.DisplayName,
                    request.Facility,
                    request.Zone,
                    request.SubZone,
                    request.NodeId,
                    request.Category,
                    request.DataType,
                    request.Unit,
                    request.ExpectedMinimum,
                    request.ExpectedMaximum,
                    TimeSpan.FromSeconds(
                        request.PublishingIntervalSeconds));

            SensorProfile? existingSensor =
                await _sensorRepository
                    .GetByDeviceIdentifierAsync(
                        sensor.Identifier,
                        cancellationToken);

            if (existingSensor is not null)
            {
                return Conflict(
                    CreateDuplicateSensorProblem(
                        request.DeviceIdentifier));
            }

            bool wasAdded =
                await _sensorRepository.AddAsync(
                    sensor,
                    cancellationToken);

            if (!wasAdded)
            {
                return Conflict(
                    CreateDuplicateSensorProblem(
                        request.DeviceIdentifier));
            }

            SensorProfileResponse response =
                SensorProfileResponse.FromDomain(sensor);

            string sensorLocation =
                $"/api/sensors/{sensor.Id:D}";

            return Created(
                sensorLocation,
                response);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(
                "sensor",
                exception.Message);

            return ValidationProblem(ModelState);
        }
    }

    private ProblemDetails CreateSensorNotFoundProblem(
        Guid sensorId)
    {
        return new ProblemDetails
        {
            Title = "Sensor profile not found",
            Detail =
                $"No sensor profile exists with identifier " +
                $"'{sensorId}'.",
            Status = StatusCodes.Status404NotFound,
            Instance = HttpContext.Request.Path
        };
    }

    private ProblemDetails CreateDuplicateSensorProblem(
        string deviceIdentifier)
    {
        return new ProblemDetails
        {
            Title = "Duplicate sensor identifier",
            Detail =
                $"A sensor using '{deviceIdentifier}' " +
                $"is already registered.",
            Status = StatusCodes.Status409Conflict,
            Instance = HttpContext.Request.Path
        };
    }
}