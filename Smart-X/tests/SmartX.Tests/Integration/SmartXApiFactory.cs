using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SmartX.Tests.Integration;

public sealed class SmartXApiFactory : WebApplicationFactory<global::Program>
{
    private readonly string _storageRoot;
    private readonly string _encryptionKey;

    public SmartXApiFactory()
    {
        _storageRoot = Path.Combine(
            Path.GetTempPath(),
            "smartx-integration-tests",
            Guid.NewGuid().ToString("N"));

        _encryptionKey =
            Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32));
    }

    /// <summary>
    /// Exposes the isolated attachment directory to integration tests.
    /// </summary>
    public string StorageRoot => _storageRoot;

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
            (_, configuration) =>
            {
                Dictionary<string, string?> settings =
                    new()
                    {
                        ["AttachmentStorage:RootPath"] =
                            _storageRoot,

                        ["AttachmentStorage:EncryptionKey"] =
                            _encryptionKey
                    };

                configuration.AddInMemoryCollection(settings);
            });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

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
            // A temporary test file may still be releasing its handle.
        }
        catch (UnauthorizedAccessException)
        {
            // Cleanup failure must not hide the actual test result.
        }
    }
}