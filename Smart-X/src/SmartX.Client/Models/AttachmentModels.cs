namespace SmartX.Client.Models;

/// <summary>
/// Attachment categories matching the Smart-X domain values.
/// </summary>
public enum AttachmentCategoryOption
{
    Configuration = 1,
    Photo = 2,
    DiagnosticLog = 3
}

/// <summary>
/// Represents an attachment successfully uploaded during this session.
/// </summary>
public sealed record UploadedAttachmentDisplay(
    string FileName,
    string Category,
    long SizeBytes,
    DateTimeOffset UploadedAt)
{
    public string FormattedSize =>
        SizeBytes switch
        {
            >= 1_048_576 =>
                $"{SizeBytes / 1_048_576d:0.00} MB",

            >= 1_024 =>
                $"{SizeBytes / 1_024d:0.00} KB",

            _ =>
                $"{SizeBytes} bytes"
        };
}