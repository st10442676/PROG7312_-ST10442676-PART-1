using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SmartX.Api.Services.Attachments;

namespace SmartX.Tests.Integration;

/// <summary>
/// Creates an isolated Smart-X API application for integration tests.
/// </summary>
public sealed class SmartXApiFactory :
    WebApplicationFactory<global::Program>
{
    private const long TestMaximumFileSizeBytes =
        10 * 1024 * 1024;

    private readonly string _storageRoot;
    private readonly string _encryptionKey;

    public SmartXApiFactory()
    {
        _storageRoot =
            Path.Combine(
                Path.GetTempPath(),
                "smartx-integration-tests",
                Guid.NewGuid().ToString("N"));

        _encryptionKey =
            Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32));
    }

    /// <summary>
    /// Exposes the isolated storage directory for encryption tests.
    /// </summary>
    public string StorageRoot =>
        _storageRoot;

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment(
            "Testing");

        builder.ConfigureAppConfiguration(
            (_, configurationBuilder) =>
            {
                Dictionary<string, string?> settings =
                    new()
                    {
                        ["AttachmentStorage:RootPath"] =
                            _storageRoot,

                        ["AttachmentStorage:MaximumFileSizeBytes"] =
                            TestMaximumFileSizeBytes.ToString(),

                        ["AttachmentStorage:EncryptionKey"] =
                            _encryptionKey
                    };

                configurationBuilder.AddInMemoryCollection(
                    settings);
            });

        builder.ConfigureServices(
            services =>
            {
                services.RemoveAll<
                    IEncryptedAttachmentStorage>();

                services.AddSingleton<
                    IEncryptedAttachmentStorage>(
                    serviceProvider =>
                    {
                        IHostEnvironment environment =
                            serviceProvider
                                .GetRequiredService<
                                    IHostEnvironment>();

                        AttachmentStorageOptions storageOptions =
                            new()
                            {
                                RootPath =
                                    _storageRoot,

                                MaximumFileSizeBytes =
                                    TestMaximumFileSizeBytes,

                                EncryptionKey =
                                    _encryptionKey
                            };

                        return new AesEncryptedAttachmentStorage(
                            Options.Create(storageOptions),
                            environment);
                    });
            });
    }

    protected override void Dispose(
        bool disposing)
    {
        base.Dispose(
            disposing);

        if (!disposing ||
            !Directory.Exists(_storageRoot))
        {
            return;
        }

        try
        {
            Directory.Delete(
                _storageRoot,
                recursive: true);
        }
        catch (IOException)
        {
            // Test cleanup must not hide the actual test outcome.
        }
        catch (UnauthorizedAccessException)
        {
            // Test cleanup must not hide the actual test outcome.
        }
    }
}