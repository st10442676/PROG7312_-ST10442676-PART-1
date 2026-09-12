using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SmartX.Tests.Integration;

/// <summary>
/// Verifies the Smart-X backend through its public HTTP API.
/// </summary>
public sealed class SmartXApiIntegrationTests :
    IClassFixture<SmartXApiFactory>
{
    private readonly SmartXApiFactory _factory;
    private readonly HttpClient _client;

    public SmartXApiIntegrationTests(
        SmartXApiFactory factory)
    {
        _factory = factory;

        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress =
                    new Uri("https://localhost")
            });
    }

    [Fact]
    public async Task RegisterSensor_WithValidRequest_ReturnsCreatedSensor()
    {
        object registration =
            CreateFloatSensorRegistration();

        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/sensors",
                registration);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using JsonDocument responseDocument =
            await ReadJsonAsync(response);

        JsonElement sensor =
            responseDocument.RootElement;

        Assert.NotEqual(
            Guid.Empty,
            sensor.GetProperty("id").GetGuid());

        Assert.Equal(
            "Environmental",
            sensor.GetProperty("category").GetString());

        Assert.Equal(
            "FloatingPoint",
            sensor.GetProperty("dataType").GetString());

        Assert.Equal(
            "Pending",
            sensor.GetProperty("connectionStatus").GetString());
    }

    [Fact]
    public async Task RegisterSensor_WithDuplicateIdentifier_ReturnsConflict()
    {
        string duplicateIdentifier =
            $"ESP32-DUPLICATE-{Guid.NewGuid():N}";

        object registration =
            CreateFloatSensorRegistration(
                duplicateIdentifier);

        HttpResponseMessage firstResponse =
            await _client.PostAsJsonAsync(
                "/api/sensors",
                registration);

        HttpResponseMessage secondResponse =
            await _client.PostAsJsonAsync(
                "/api/sensors",
                registration);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task IngestTelemetry_NormalAndAbnormalReadings_AreClassified()
    {
        Guid sensorId =
            await RegisterFloatSensorAsync();

        object normalReading = new
        {
            sequenceNumber = 1,
            value = 24.5f,
            capturedAtUtc =
                DateTimeOffset.UtcNow.AddSeconds(-1)
        };

        HttpResponseMessage normalResponse =
            await _client.PostAsJsonAsync(
                $"/api/sensors/{sensorId}/telemetry/float",
                normalReading);

        Assert.Equal(
            HttpStatusCode.Created,
            normalResponse.StatusCode);

        using JsonDocument normalDocument =
            await ReadJsonAsync(normalResponse);

        Assert.Equal(
            "Normal",
            normalDocument.RootElement
                .GetProperty("healthState")
                .GetString());

        Assert.Equal(
            "Accepted",
            normalDocument.RootElement
                .GetProperty("ingestionState")
                .GetString());

        object abnormalReading = new
        {
            sequenceNumber = 2,
            value = 79.5f,
            capturedAtUtc =
                DateTimeOffset.UtcNow.AddSeconds(-1)
        };

        HttpResponseMessage abnormalResponse =
            await _client.PostAsJsonAsync(
                $"/api/sensors/{sensorId}/telemetry/float",
                abnormalReading);

        Assert.Equal(
            HttpStatusCode.Created,
            abnormalResponse.StatusCode);

        using JsonDocument abnormalDocument =
            await ReadJsonAsync(abnormalResponse);

        Assert.Equal(
            "OutOfRange",
            abnormalDocument.RootElement
                .GetProperty("healthState")
                .GetString());

        Assert.Equal(
            "Flagged",
            abnormalDocument.RootElement
                .GetProperty("ingestionState")
                .GetString());

        HttpResponseMessage historyResponse =
            await _client.GetAsync(
                $"/api/sensors/{sensorId}/telemetry");

        Assert.Equal(
            HttpStatusCode.OK,
            historyResponse.StatusCode);

        using JsonDocument historyDocument =
            await ReadJsonAsync(historyResponse);

        Assert.Equal(
            2,
            historyDocument.RootElement
                .GetProperty("floatingPointReadings")
                .GetArrayLength());
    }

    [Fact]
    public async Task UploadAttachment_WithHardwareLog_EncryptsAndStoresFile()
    {
        Guid sensorId =
            await RegisterFloatSensorAsync();

        const string logText =
            "Device boot completed. Wi-Fi connected. " +
            "Telemetry publishing started.";

        byte[] logBytes =
            Encoding.UTF8.GetBytes(logText);

        using ByteArrayContent fileContent =
            new(logBytes);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue("text/plain");

        using MultipartFormDataContent form = new();

        form.Add(
            new StringContent("HardwareLog"),
            "Category");

        form.Add(
            fileContent,
            "File",
            "hardware.log");

        HttpResponseMessage uploadResponse =
            await _client.PostAsync(
                $"/api/sensors/{sensorId}/attachments",
                form);

        Assert.Equal(
            HttpStatusCode.Created,
            uploadResponse.StatusCode);

        using JsonDocument uploadDocument =
            await ReadJsonAsync(uploadResponse);

        Assert.Equal(
            "HardwareLog",
            uploadDocument.RootElement
                .GetProperty("category")
                .GetString());

        Assert.Equal(
            "hardware.log",
            uploadDocument.RootElement
                .GetProperty("originalFileName")
                .GetString());

        string[] encryptedFiles =
            Directory.GetFiles(
                _factory.StorageRoot,
                "*.sxenc",
                SearchOption.TopDirectoryOnly);

        Assert.NotEmpty(encryptedFiles);

        byte[] encryptedBytes =
            await File.ReadAllBytesAsync(
                encryptedFiles[^1]);

        Assert.True(
            encryptedBytes.Length > logBytes.Length);

        Assert.Equal(
            "SMARTX01",
            Encoding.ASCII.GetString(
                encryptedBytes,
                0,
                8));

        Assert.DoesNotContain(
            logText,
            Encoding.Latin1.GetString(
                encryptedBytes),
            StringComparison.Ordinal);

        HttpResponseMessage listResponse =
            await _client.GetAsync(
                $"/api/sensors/{sensorId}/attachments");

        Assert.Equal(
            HttpStatusCode.OK,
            listResponse.StatusCode);
    }

    private async Task<Guid> RegisterFloatSensorAsync()
    {
        HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/sensors",
                CreateFloatSensorRegistration());

        response.EnsureSuccessStatusCode();

        using JsonDocument responseDocument =
            await ReadJsonAsync(response);

        return responseDocument.RootElement
            .GetProperty("id")
            .GetGuid();
    }

    private static object CreateFloatSensorRegistration(
        string? deviceIdentifier = null)
    {
        return new
        {
            deviceIdentifier =
                deviceIdentifier ??
                $"ESP32-TEST-{Guid.NewGuid():N}",

            displayName =
                "Integration Test Temperature Sensor",

            facility =
                "Test Facility",

            zone =
                "Zone 1",

            subZone =
                "Sub-Zone A",

            nodeId =
                "Test Node 01",

            category =
                "Environmental",

            dataType =
                "FloatingPoint",

            unit =
                "°C",

            expectedMinimum =
                18.0,

            expectedMaximum =
                30.0,

            publishingIntervalSeconds =
                30
        };
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response)
    {
        string json =
            await response.Content.ReadAsStringAsync();

        return JsonDocument.Parse(json);
    }
}