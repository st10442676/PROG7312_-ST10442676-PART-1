using SmartX.Domain.Enums;

namespace SmartX.Domain.Attachments;

/// <summary>
/// Contains metadata for an encrypted sensor attachment.
/// </summary>
public sealed record SensorAttachment(
    Guid Id,
    Guid SensorId,
    AttachmentCategory Category,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    string EncryptedStorageName,
    DateTimeOffset UploadedAtUtc);