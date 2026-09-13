using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace SmartX.Tests.Integration;

public sealed class SmartXApiIntegrationTests :
    IClassFixture<SmartXApiFactory>
{
    private readonly SmartXApiFactory _factory;
    private readonly HttpClient _client;

    public SmartXApiIntegrationTests(
        SmartXApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GatewayHealth_WhenRequested_ReturnsSuccess()
    {
        using HttpResponseMessage response =
            await _client.GetAsync(
                "/api/gateway/health");

        string responseBody =
            await response.Content.ReadAsStringAsync();

        Assert.True(
            response.IsSuccessStatusCode,
            CreateFailureMessage(
                HttpStatusCode.OK,
                response,
                responseBody));

        Assert.False(
            string.IsNullOrWhiteSpace(responseBody));
    }

    [Fact]
    public async Task
        RegisterSensor_WithValidRequest_ReturnsCreatedSensor()
    {
        object request =
            CreateRegistrationRequest(
                CreateUniqueDeviceIdentifier());

        using HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/sensors",
                request);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            CreateFailureMessage(
                HttpStatusCode.Created,
                response,
                responseBody));

        Guid sensorId =
            ExtractSensorId(responseBody);

        Assert.NotEqual(
            Guid.Empty,
            sensorId);
    }

    [Fact]
    public async Task
        RegisterSensor_WithDuplicateIdentifier_ReturnsConflict()
    {
        string deviceIdentifier =
            CreateUniqueDeviceIdentifier();

        object firstRequest =
            CreateRegistrationRequest(
                deviceIdentifier);

        using HttpResponseMessage firstResponse =
            await _client.PostAsJsonAsync(
                "/api/sensors",
                firstRequest);

        string firstResponseBody =
            await firstResponse.Content.ReadAsStringAsync();

        Assert.True(
            firstResponse.StatusCode == HttpStatusCode.Created,
            CreateFailureMessage(
                HttpStatusCode.Created,
                firstResponse,
                firstResponseBody));

        object duplicateRequest =
            CreateRegistrationRequest(
                deviceIdentifier);

        using HttpResponseMessage duplicateResponse =
            await _client.PostAsJsonAsync(
                "/api/sensors",
                duplicateRequest);

        string duplicateResponseBody =
            await duplicateResponse.Content.ReadAsStringAsync();

        Assert.True(
            duplicateResponse.StatusCode == HttpStatusCode.Conflict,
            CreateFailureMessage(
                HttpStatusCode.Conflict,
                duplicateResponse,
                duplicateResponseBody));
    }

    [Fact]
    public async Task
        IngestTelemetry_NormalAndAbnormalReadings_AreClassified()
    {
        Guid sensorId =
            await RegisterFloatSensorAsync();

        DateTimeOffset firstTimestamp =
            DateTimeOffset.UtcNow;

        object normalReading =
            CreateFloatTelemetryRequest(
                value: 24.8f,
                sequenceNumber: 1,
                timestamp: firstTimestamp);

        using HttpResponseMessage normalResponse =
            await _client.PostAsJsonAsync(
                $"/api/sensors/{sensorId}/telemetry/float",
                normalReading);

        string normalResponseBody =
            await normalResponse.Content.ReadAsStringAsync();

        Assert.True(
            normalResponse.StatusCode == HttpStatusCode.Created,
            CreateFailureMessage(
                HttpStatusCode.Created,
                normalResponse,
                normalResponseBody));

        object abnormalReading =
            CreateFloatTelemetryRequest(
                value: 99.9f,
                sequenceNumber: 2,
                timestamp: firstTimestamp.AddMilliseconds(1));

        using HttpResponseMessage abnormalResponse =
            await _client.PostAsJsonAsync(
                $"/api/sensors/{sensorId}/telemetry/float",
                abnormalReading);

        string abnormalResponseBody =
            await abnormalResponse.Content.ReadAsStringAsync();

        Assert.True(
            abnormalResponse.StatusCode == HttpStatusCode.Created,
            CreateFailureMessage(
                HttpStatusCode.Created,
                abnormalResponse,
                abnormalResponseBody));

        using HttpResponseMessage historyResponse =
            await _client.GetAsync(
                $"/api/sensors/{sensorId}/telemetry");

        string historyResponseBody =
            await historyResponse.Content.ReadAsStringAsync();

        Assert.True(
            historyResponse.StatusCode == HttpStatusCode.OK,
            CreateFailureMessage(
                HttpStatusCode.OK,
                historyResponse,
                historyResponseBody));

        using JsonDocument historyDocument =
            ParseRequiredJson(
                historyResponseBody,
                "telemetry history");

        JsonElement root =
            historyDocument.RootElement;

        Assert.True(
            TryGetPropertyIgnoringCase(
                root,
                "floatingPointReadings",
                out JsonElement floatingPointReadings),
            $"The telemetry history response did not contain " +
            $"'floatingPointReadings'. " +
            $"API response: {historyResponseBody}");

        Assert.Equal(
            JsonValueKind.Array,
            floatingPointReadings.ValueKind);

        JsonElement[] readings =
            floatingPointReadings
                .EnumerateArray()
                .ToArray();

        Assert.True(
            readings.Length >= 2,
            $"Expected at least two floating-point readings but " +
            $"received {readings.Length}. " +
            $"API response: {historyResponseBody}");

        JsonElement firstReading =
            readings[0];

        JsonElement secondReading =
            readings[1];

        Assert.Equal(
            "Normal",
            GetRequiredStringProperty(
                firstReading,
                "healthState"));

        Assert.Equal(
            "Accepted",
            GetRequiredStringProperty(
                firstReading,
                "ingestionState"));

        Assert.Equal(
            "OutOfRange",
            GetRequiredStringProperty(
                secondReading,
                "healthState"));

        Assert.Equal(
            "Flagged",
            GetRequiredStringProperty(
                secondReading,
                "ingestionState"));
    }

    [Fact]
    public async Task
        UploadAttachment_WithHardwareLog_EncryptsAndStoresFile()
    {
        Guid sensorId =
            await RegisterFloatSensorAsync();

        const string originalPlainText =
            "SMART-X HARDWARE LOG - PRIVATE SENSOR INFORMATION";

        byte[] fileBytes =
            Encoding.UTF8.GetBytes(
                originalPlainText);

        using MultipartFormDataContent multipartContent =
            new();

        using ByteArrayContent fileContent =
            new(fileBytes);

        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(
                "text/plain");

        multipartContent.Add(
            fileContent,
            "File",
            "hardware-diagnostic.log");

        multipartContent.Add(
            new StringContent("3"),
            "Category");

        multipartContent.Add(
            new StringContent(
                "Hardware diagnostic log created by integration testing."),
            "Description");

        using HttpResponseMessage response =
            await _client.PostAsync(
                $"/api/sensors/{sensorId}/attachments",
                multipartContent);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            CreateFailureMessage(
                HttpStatusCode.Created,
                response,
                responseBody));

        Assert.True(
            Directory.Exists(_factory.StorageRoot),
            $"The configured test storage directory was not created: " +
            $"{_factory.StorageRoot}");

        string[] storedFiles =
            Directory.GetFiles(
                _factory.StorageRoot,
                "*",
                SearchOption.AllDirectories);

        Assert.NotEmpty(
            storedFiles);

        foreach (string storedFile in storedFiles)
        {
            byte[] encryptedBytes =
                await File.ReadAllBytesAsync(
                    storedFile);

            string storedText =
                Encoding.UTF8.GetString(
                    encryptedBytes);

            Assert.DoesNotContain(
                originalPlainText,
                storedText);
        }
    }

    private async Task<Guid> RegisterFloatSensorAsync()
    {
        object request =
            CreateRegistrationRequest(
                CreateUniqueDeviceIdentifier());

        using HttpResponseMessage response =
            await _client.PostAsJsonAsync(
                "/api/sensors",
                request);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        Assert.True(
            response.StatusCode == HttpStatusCode.Created,
            CreateFailureMessage(
                HttpStatusCode.Created,
                response,
                responseBody));

        return ExtractSensorId(
            responseBody);
    }

    private static object CreateRegistrationRequest(
        string deviceIdentifier)
    {
        return new
        {
            deviceIdentifier,

            displayName =
                "Integration Test Environmental Sensor",

            facility =
                "Durban Integration Facility",

            unit =
                "Level 2",

            nodeId =
                "LAB-NODE-01",

            zone =
                "Environmental Monitoring Zone",

            subZone =
                "Environmental Test Bay",

            category = 1,

            dataType = 1,

            publishingIntervalSeconds = 30,

            expectedMinimum = 0.0,

            expectedMaximum = 50.0
        };
    }

    private static object CreateFloatTelemetryRequest(
        float value,
        long sequenceNumber,
        DateTimeOffset timestamp)
    {
        return new
        {
            value,

            unit =
                "°C",

            sequenceNumber,

            capturedAtUtc =
                timestamp,

            timestampUtc =
                timestamp,

            timestamp
        };
    }

    private static string CreateUniqueDeviceIdentifier()
    {
        byte[] bytes =
            Guid.NewGuid().ToByteArray();

        return
            $"02:{bytes[0]:X2}:{bytes[1]:X2}:" +
            $"{bytes[2]:X2}:{bytes[3]:X2}:{bytes[4]:X2}";
    }

    private static Guid ExtractSensorId(
        string responseBody)
    {
        using JsonDocument document =
            ParseRequiredJson(
                responseBody,
                "sensor registration");

        JsonElement root =
            document.RootElement;

        string[] possiblePropertyNames =
        [
            "id",
            "sensorId"
        ];

        foreach (string propertyName in possiblePropertyNames)
        {
            if (
                TryGetPropertyIgnoringCase(
                    root,
                    propertyName,
                    out JsonElement property) &&
                property.ValueKind == JsonValueKind.String &&
                Guid.TryParse(
                    property.GetString(),
                    out Guid sensorId))
            {
                return sensorId;
            }
        }

        throw new Xunit.Sdk.XunitException(
            $"The successful registration response did not contain a " +
            $"valid 'id' or 'sensorId'. " +
            $"API response: {responseBody}");
    }

    private static JsonDocument ParseRequiredJson(
        string responseBody,
        string operationName)
    {
        Assert.False(
            string.IsNullOrWhiteSpace(responseBody),
            $"The {operationName} API returned an empty response body.");

        try
        {
            return JsonDocument.Parse(
                responseBody);
        }
        catch (JsonException exception)
        {
            throw new Xunit.Sdk.XunitException(
                $"The {operationName} API returned invalid JSON. " +
                $"Response: {responseBody}. " +
                $"Parser error: {exception.Message}");
        }
    }

    private static string GetRequiredStringProperty(
        JsonElement element,
        string propertyName)
    {
        Assert.True(
            TryGetPropertyIgnoringCase(
                element,
                propertyName,
                out JsonElement property),
            $"The response did not contain the required " +
            $"'{propertyName}' property. Response item: {element}");

        Assert.Equal(
            JsonValueKind.String,
            property.ValueKind);

        string? value =
            property.GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(value),
            $"The '{propertyName}' property was empty.");

        return value!;
    }

    private static bool TryGetPropertyIgnoringCase(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (
                JsonProperty property
                in element.EnumerateObject())
            {
                if (
                    string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string CreateFailureMessage(
        HttpStatusCode expectedStatusCode,
        HttpResponseMessage response,
        string responseBody)
    {
        string readableBody =
            string.IsNullOrWhiteSpace(responseBody)
                ? "<empty response body>"
                : responseBody;

        return
            $"Expected HTTP {(int)expectedStatusCode} " +
            $"{expectedStatusCode}, but received " +
            $"{(int)response.StatusCode} {response.StatusCode}. " +
            $"API response: {readableBody}";
    }
}