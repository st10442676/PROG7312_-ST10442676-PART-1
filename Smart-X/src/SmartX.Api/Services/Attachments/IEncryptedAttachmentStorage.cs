using SmartX.Domain.Attachments;
using SmartX.Domain.Enums;

namespace SmartX.Api.Services.Attachments;

/// <summary>
/// Defines encrypted, asynchronous attachment-storage operations.
/// </summary>
public interface IEncryptedAttachmentStorage
{
    Task<SensorAttachment> SaveAsync(
        Guid sensorId,
        AttachmentCategory category,
        string originalFileName,
        string contentType,
        long declaredSizeBytes,
        Stream content,
        CancellationToken cancellationToken = default);

    IReadOnlyList<SensorAttachment> GetBySensor(
        Guid sensorId);
}