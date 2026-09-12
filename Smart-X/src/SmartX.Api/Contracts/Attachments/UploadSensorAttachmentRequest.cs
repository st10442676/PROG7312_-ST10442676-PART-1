using System.ComponentModel.DataAnnotations;
using SmartX.Domain.Enums;

namespace SmartX.Api.Contracts.Attachments;

/// <summary>
/// Contains a multipart sensor-attachment upload.
/// </summary>
public sealed class UploadSensorAttachmentRequest
{
    /// <summary>
    /// Gets the uploaded configuration, photograph or log file.
    /// </summary>
    [Required]
    public IFormFile? File { get; init; }

    /// <summary>
    /// Gets the purpose of the uploaded attachment.
    /// </summary>
    [EnumDataType(typeof(AttachmentCategory))]
    public AttachmentCategory Category { get; init; }
}