namespace SmartX.Api.Services.Attachments;

/// <summary>
/// Contains secure attachment-storage configuration.
/// </summary>
public sealed class AttachmentStorageOptions
{
    public const string SectionName = "AttachmentStorage";

    public string RootPath { get; init; } =
        "App_Data/EncryptedAttachments";

    public long MaximumFileSizeBytes { get; init; } =
        10 * 1024 * 1024;

    /// <summary>
    /// Gets the Base64-encoded 256-bit encryption key.
    /// This must be supplied through user secrets or an environment variable.
    /// </summary>
    public string EncryptionKey { get; init; } = string.Empty;
}