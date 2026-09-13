using Microsoft.AspNetCore.Mvc;
using SmartX.Api.Contracts.Attachments;
using SmartX.Api.Services.Attachments;
using SmartX.Domain.Attachments;
using SmartX.Domain.Entities;
using SmartX.Domain.Repositories;

namespace SmartX.Api.Controllers;

/// <summary>
/// Manages encrypted files attached to Smart-X sensor profiles.
/// </summary>
[ApiController]
[Route("api/sensors/{sensorId:guid}/attachments")]
public sealed class AttachmentsController : ControllerBase
{
    private const long MaximumRequestSizeBytes =
        11 * 1024 * 1024;

    private readonly ISensorProfileRepository _sensorRepository;

    private readonly IEncryptedAttachmentStorage
        _attachmentStorage;

    private readonly ILogger<AttachmentsController>
        _logger;

    private readonly IHostEnvironment _environment;

    public AttachmentsController(
        ISensorProfileRepository sensorRepository,
        IEncryptedAttachmentStorage attachmentStorage,
        ILogger<AttachmentsController> logger,
        IHostEnvironment environment)
    {
        _sensorRepository =
            sensorRepository
            ?? throw new ArgumentNullException(
                nameof(sensorRepository));

        _attachmentStorage =
            attachmentStorage
            ?? throw new ArgumentNullException(
                nameof(attachmentStorage));

        _logger =
            logger
            ?? throw new ArgumentNullException(
                nameof(logger));

        _environment =
            environment
            ?? throw new ArgumentNullException(
                nameof(environment));
    }

    /// <summary>
    /// Uploads, validates and encrypts an attachment for a
    /// registered Smart-X sensor.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumRequestSizeBytes)]
    [ProducesResponseType<SensorAttachmentResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(
        StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SensorAttachmentResponse>>
        UploadAsync(
            Guid sensorId,
            [FromForm] UploadSensorAttachmentRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            SensorProfile? sensor =
                await _sensorRepository.GetByIdAsync(
                    sensorId,
                    cancellationToken);

            if (sensor is null)
            {
                return NotFound(
                    CreateSensorNotFoundProblem(
                        sensorId));
            }

            if (request.File is null)
            {
                ModelState.AddModelError(
                    nameof(request.File),
                    "An attachment file is required.");

                return ValidationProblem(
                    ModelState);
            }

            if (request.File.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(request.File),
                    "The attachment file cannot be empty.");

                return ValidationProblem(
                    ModelState);
            }

            await using Stream content =
                request.File.OpenReadStream();

            SensorAttachment attachment =
                await _attachmentStorage.SaveAsync(
                    sensorId,
                    request.Category,
                    request.File.FileName,
                    request.File.ContentType,
                    request.File.Length,
                    content,
                    cancellationToken);

            SensorAttachmentResponse response =
                SensorAttachmentResponse.FromDomain(
                    attachment);

            string attachmentLocation =
                $"/api/sensors/{sensorId:D}/attachments";

            return Created(
                attachmentLocation,
                response);
        }
        catch (InvalidDataException exception)
        {
            ModelState.AddModelError(
                nameof(request.File),
                exception.Message);

            return ValidationProblem(
                ModelState);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(
                "attachment",
                exception.Message);

            return ValidationProblem(
                ModelState);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Encrypted attachment upload failed for sensor {SensorId}.",
                sensorId);

            string detail =
                _environment.IsDevelopment() ||
                _environment.IsEnvironment("Testing")
                    ? exception.ToString()
                    : "The encrypted attachment could not be stored.";

            return Problem(
                title:
                    "Encrypted attachment storage failed",

                detail:
                    detail,

                statusCode:
                    StatusCodes.Status500InternalServerError,

                instance:
                    HttpContext.Request.Path);
        }
    }

    /// <summary>
    /// Returns metadata for all files attached to a registered sensor.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<
        IReadOnlyList<SensorAttachmentResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<IReadOnlyList<SensorAttachmentResponse>>>
        GetAllAsync(
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
                CreateSensorNotFoundProblem(
                    sensorId));
        }

        IReadOnlyList<SensorAttachment> attachments =
            _attachmentStorage.GetBySensor(
                sensorId);

        List<SensorAttachmentResponse> response =
            attachments
                .Select(
                    SensorAttachmentResponse.FromDomain)
                .ToList();

        return Ok(
            response);
    }

    private ProblemDetails CreateSensorNotFoundProblem(
        Guid sensorId)
    {
        return new ProblemDetails
        {
            Title =
                "Sensor profile not found",

            Detail =
                $"No sensor profile exists with " +
                $"identifier '{sensorId}'.",

            Status =
                StatusCodes.Status404NotFound,

            Instance =
                HttpContext.Request.Path
        };
    }
}