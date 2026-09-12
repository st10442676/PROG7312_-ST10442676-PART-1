using System.Buffers;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SmartX.Domain.Attachments;
using SmartX.Domain.Enums;

namespace SmartX.Api.Services.Attachments;

/// <summary>
/// Stores complete attachment streams using AES-256 encryption.
/// </summary>
public sealed class AesEncryptedAttachmentStorage :
    IEncryptedAttachmentStorage
{
    private const int StreamBufferSize = 81_920;

    private static readonly byte[] FileHeader =
        Encoding.ASCII.GetBytes("SMARTX01");

    private readonly string _storageRoot;
    private readonly long _maximumFileSizeBytes;
    private readonly byte[] _encryptionKey;

    private readonly ConcurrentDictionary<
        Guid,
        ConcurrentDictionary<Guid, SensorAttachment>>
        _attachmentsBySensor = new();

    public AesEncryptedAttachmentStorage(
        IOptions<AttachmentStorageOptions> options,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(environment);

        AttachmentStorageOptions settings =
            options.Value;

        if (settings.MaximumFileSizeBytes <= 0)
        {
            throw new InvalidOperationException(
                "Maximum attachment size must be greater than zero.");
        }

        _maximumFileSizeBytes =
            settings.MaximumFileSizeBytes;

        _encryptionKey =
            ParseEncryptionKey(settings.EncryptionKey);

        _storageRoot =
            Path.IsPathRooted(settings.RootPath)
                ? settings.RootPath
                : Path.Combine(
                    environment.ContentRootPath,
                    settings.RootPath);

        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<SensorAttachment> SaveAsync(
        Guid sensorId,
        AttachmentCategory category,
        string originalFileName,
        string contentType,
        long declaredSizeBytes,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (sensorId == Guid.Empty)
        {
            throw new ArgumentException(
                "A valid sensor identifier is required.",
                nameof(sensorId));
        }

        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "The attachment category is not supported.");
        }

        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanRead)
        {
            throw new ArgumentException(
                "The attachment stream must be readable.",
                nameof(content));
        }

        if (declaredSizeBytes <= 0)
        {
            throw new InvalidDataException(
                "The attachment cannot be empty.");
        }

        if (declaredSizeBytes > _maximumFileSizeBytes)
        {
            throw new InvalidDataException(
                $"The attachment exceeds the maximum size of " +
                $"{_maximumFileSizeBytes} bytes.");
        }

        string safeOriginalFileName =
            NormaliseFileName(originalFileName);

        ValidateFileExtension(
            safeOriginalFileName,
            category);

        Guid attachmentId =
            Guid.NewGuid();

        string encryptedStorageName =
            $"{attachmentId:N}.sxenc";

        string finalPath =
            Path.Combine(
                _storageRoot,
                encryptedStorageName);

        string temporaryPath =
            $"{finalPath}.uploading";

        try
        {
            long actualSizeBytes =
                await EncryptToFileAsync(
                    content,
                    temporaryPath,
                    cancellationToken);

            if (actualSizeBytes == 0)
            {
                throw new InvalidDataException(
                    "The attachment stream was empty.");
            }

            File.Move(
                temporaryPath,
                finalPath);

            SensorAttachment attachment = new(
                attachmentId,
                sensorId,
                category,
                safeOriginalFileName,
                NormaliseContentType(contentType),
                actualSizeBytes,
                encryptedStorageName,
                DateTimeOffset.UtcNow);

            ConcurrentDictionary<Guid, SensorAttachment>
                sensorAttachments =
                    _attachmentsBySensor.GetOrAdd(
                        sensorId,
                        static _ => new ConcurrentDictionary<
                            Guid,
                            SensorAttachment>());

            if (!sensorAttachments.TryAdd(
                    attachment.Id,
                    attachment))
            {
                File.Delete(finalPath);

                throw new InvalidOperationException(
                    "The attachment metadata could not be recorded.");
            }

            return attachment;
        }
        catch
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            throw;
        }
    }

    public IReadOnlyList<SensorAttachment> GetBySensor(
        Guid sensorId)
    {
        if (!_attachmentsBySensor.TryGetValue(
                sensorId,
                out ConcurrentDictionary<
                    Guid,
                    SensorAttachment>? attachments))
        {
            return Array.Empty<SensorAttachment>();
        }

        return attachments.Values
            .OrderByDescending(
                attachment => attachment.UploadedAtUtc)
            .ToList()
            .AsReadOnly();
    }

    private async Task<long> EncryptToFileAsync(
        Stream input,
        string temporaryPath,
        CancellationToken cancellationToken)
    {
        using Aes aes = Aes.Create();

        aes.KeySize = 256;
        aes.Key = _encryptionKey;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        await using FileStream encryptedFile =
            new(
                temporaryPath,
                new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    BufferSize = StreamBufferSize,
                    Options =
                        FileOptions.Asynchronous |
                        FileOptions.SequentialScan
                });

        await encryptedFile.WriteAsync(
            FileHeader,
            cancellationToken);

        await encryptedFile.WriteAsync(
            aes.IV,
            cancellationToken);

        using ICryptoTransform encryptor =
            aes.CreateEncryptor();

        await using CryptoStream encryptionStream =
            new(
                encryptedFile,
                encryptor,
                CryptoStreamMode.Write,
                leaveOpen: true);

        long bytesCopied =
            await CopyWithLimitAsync(
                input,
                encryptionStream,
                cancellationToken);

        encryptionStream.FlushFinalBlock();

        await encryptedFile.FlushAsync(
            cancellationToken);

        return bytesCopied;
    }

    private async Task<long> CopyWithLimitAsync(
        Stream source,
        Stream destination,
        CancellationToken cancellationToken)
    {
        byte[] buffer =
            ArrayPool<byte>.Shared.Rent(
                StreamBufferSize);

        long totalBytesRead = 0;

        try
        {
            while (true)
            {
                int bytesRead =
                    await source.ReadAsync(
                        buffer.AsMemory(
                            0,
                            StreamBufferSize),
                        cancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                totalBytesRead += bytesRead;

                if (totalBytesRead >
                    _maximumFileSizeBytes)
                {
                    throw new InvalidDataException(
                        $"The attachment exceeds the maximum size of " +
                        $"{_maximumFileSizeBytes} bytes.");
                }

                await destination.WriteAsync(
                    buffer.AsMemory(
                        0,
                        bytesRead),
                    cancellationToken);
            }

            return totalBytesRead;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(
                buffer,
                clearArray: true);
        }
    }

    private static byte[] ParseEncryptionKey(
        string encodedKey)
    {
        if (string.IsNullOrWhiteSpace(encodedKey))
        {
            throw new InvalidOperationException(
                "AttachmentStorage:EncryptionKey is missing. " +
                "Configure it through .NET user secrets.");
        }

        try
        {
            byte[] key =
                Convert.FromBase64String(encodedKey);

            if (key.Length != 32)
            {
                throw new InvalidOperationException(
                    "The attachment encryption key must contain " +
                    "exactly 32 bytes for AES-256.");
            }

            return key;
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "The attachment encryption key must be valid Base64.",
                exception);
        }
    }

    private static string NormaliseFileName(
        string originalFileName)
    {
        string safeName =
            Path.GetFileName(originalFileName).Trim();

        if (string.IsNullOrWhiteSpace(safeName))
        {
            throw new InvalidDataException(
                "A valid attachment filename is required.");
        }

        if (safeName.Length > 255)
        {
            throw new InvalidDataException(
                "The attachment filename cannot exceed 255 characters.");
        }

        return safeName;
    }

    private static string NormaliseContentType(
        string contentType)
    {
        return string.IsNullOrWhiteSpace(contentType)
            ? "application/octet-stream"
            : contentType.Trim();
    }

    private static void ValidateFileExtension(
        string fileName,
        AttachmentCategory category)
    {
        string extension =
            Path.GetExtension(fileName)
                .ToLowerInvariant();

        string[] permittedExtensions =
            category switch
            {
                AttachmentCategory.Configuration =>
                [
                    ".json",
                    ".yaml",
                    ".yml",
                    ".cfg",
                    ".txt"
                ],

                AttachmentCategory.DeploymentPhoto =>
                [
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                ],

                AttachmentCategory.HardwareLog =>
                [
                    ".log",
                    ".txt",
                    ".csv"
                ],

                _ => []
            };

        if (!permittedExtensions.Contains(
                extension,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"File extension '{extension}' is not permitted " +
                $"for the {category} attachment category.");
        }
    }
}