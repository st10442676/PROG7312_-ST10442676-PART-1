using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SmartX.Tests.Integration;

/// <summary>
/// Creates an isolated Smart-X API instance for integration testing.
/// Each factory receives its own temporary attachment directory and
/// encryption key so that tests never modify production data.
/// </summary>
public sealed class SmartXApiFactory : WebApplicationFactory<global::Program>
{
    private readonly string _storageRoot;
    private readonly string _encryptionKey;

    public SmartXApiFactory()
    {
        _storageRoot = Path.Combine(
            Path.GetTempPath(),
            "SmartX",
            "IntegrationTests",
            Guid.NewGuid().ToString("N"));

        _encryptionKey = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(32));
    }

    /// <summary>
    /// Configures the API with an isolated testing environment.
    /// </summary>
    /// <param name="builder">
    /// The web-host builder supplied by WebApplicationFactory.
    /// </param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
            (_, configurationBuilder) =>
            {
                Dictionary<string, string?> testSettings = new()
                {
                    ["AttachmentStorage:RootPath"] = _storageRoot,
                    ["AttachmentStorage:EncryptionKey"] = _encryptionKey
                };

                configurationBuilder.AddInMemoryCollection(testSettings);
            });
    }

    /// <summary>
    /// Disposes the test server and removes temporary encrypted files
    /// created during integration testing.
    /// </summary>
    /// <param name="disposing">
    /// Indicates whether managed resources should be disposed.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || !Directory.Exists(_storageRoot))
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
            // A temporary test file may still be releasing its file handle.
            // Cleanup failure must not hide the actual integration-test result.
        }
        catch (UnauthorizedAccessException)
        {
            // Cleanup failure must not hide the actual integration-test result.
        }
    }
}