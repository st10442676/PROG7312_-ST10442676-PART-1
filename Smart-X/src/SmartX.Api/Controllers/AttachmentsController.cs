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
    private readonly IEncryptedAttachmentStorage _attachmentStorage;

    public AttachmentsController(
        ISensorProfileRepository sensorRepository,
        IEncryptedAttachmentStorage attachmentStorage)
    {
        _sensorRepository = sensorRepository;
        _attachmentStorage = attachmentStorage;
    }

    /// <summary>
    /// Uploads and encrypts an attachment for a registered sensor.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumRequestSizeBytes)]
    [ProducesResponseType<SensorAttachmentResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<SensorAttachmentResponse>>
        UploadAsync(
            Guid sensorId,
            [FromForm] UploadSensorAttachmentRequest request,
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

        if (request.File is null)
        {
            ModelState.AddModelError(
                nameof(request.File),
                "An attachment file is required.");

            return ValidationProblem(ModelState);
        }

        if (request.File.Length == 0)
        {
            ModelState.AddModelError(
                nameof(request.File),
                "The attachment file cannot be empty.");

            return ValidationProblem(ModelState);
        }

        try
        {
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

            return CreatedAtAction(
                nameof(GetAllAsync),
                new { sensorId },
                response);
        }
        catch (InvalidDataException exception)
        {
            ModelState.AddModelError(
                nameof(request.File),
                exception.Message);

            return ValidationProblem(ModelState);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(
                "attachment",
                exception.Message);

            return ValidationProblem(ModelState);
        }
    }

    /// <summary>
    /// Returns metadata for all files attached to a registered sensor.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<
        IReadOnlyList<SensorAttachmentResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
                CreateSensorNotFoundProblem(sensorId));
        }

        IReadOnlyList<SensorAttachment> attachments =
            _attachmentStorage.GetBySensor(sensorId);

        List<SensorAttachmentResponse> response =
            attachments
                .Select(
                    SensorAttachmentResponse.FromDomain)
                .ToList();

        return Ok(response);
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
}