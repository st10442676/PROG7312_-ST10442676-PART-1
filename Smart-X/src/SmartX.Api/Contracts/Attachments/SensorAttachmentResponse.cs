using SmartX.Domain.Attachments;
using SmartX.Domain.Enums;

namespace SmartX.Api.Contracts.Attachments;

/// <summary>
/// Contains safe, client-facing sensor-attachment metadata.
/// </summary>
public sealed record SensorAttachmentResponse(
    Guid Id,
    Guid SensorId,
    AttachmentCategory Category,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAtUtc)
{
    public static SensorAttachmentResponse FromDomain(
        SensorAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);

        return new SensorAttachmentResponse(
            attachment.Id,
            attachment.SensorId,
            attachment.Category,
            attachment.OriginalFileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.UploadedAtUtc);
    }
}