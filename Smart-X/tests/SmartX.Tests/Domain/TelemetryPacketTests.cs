using SmartX.Domain.Enums;
using SmartX.Domain.Telemetry;
using Xunit;

namespace SmartX.Tests.Domain;

/// <summary>
/// Verifies the behaviour of the generic TelemetryPacket&lt;T&gt; wrapper.
/// </summary>
public sealed class TelemetryPacketTests
{
    [Fact]
    public void Create_WithFloatValue_PreservesFloatingPointType()
    {
        Guid sensorId = Guid.NewGuid();

        DateTimeOffset capturedAt =
            new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

        DateTimeOffset receivedAt =
            capturedAt.AddMilliseconds(250);

        TelemetryPacket<float> packet =
            TelemetryPacket<float>.Create(
                sensorId,
                sequenceNumber: 1,
                value: 24.5f,
                unit: "°C",
                capturedAt,
                receivedAt);

        Assert.Equal(sensorId, packet.SensorId);
        Assert.Equal(1, packet.SequenceNumber);
        Assert.Equal(24.5f, packet.Value);
        Assert.Equal("Single", packet.ValueTypeName);
        Assert.True(packet.ContainsFloatingPointValue());
        Assert.False(packet.ContainsIntegerValue());
        Assert.False(packet.ContainsBooleanValue());

        Assert.Equal(
            TimeSpan.FromMilliseconds(250),
            packet.IngestionLatency);
    }

    [Fact]
    public void Create_WithIntegerValue_PreservesIntegerType()
    {
        DateTimeOffset timestamp =
            new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

        TelemetryPacket<int> packet =
            TelemetryPacket<int>.Create(
                Guid.NewGuid(),
                sequenceNumber: 2,
                value: 850,
                unit: "W",
                timestamp,
                timestamp);

        Assert.Equal(850, packet.Value);
        Assert.Equal("Int32", packet.ValueTypeName);
        Assert.True(packet.ContainsIntegerValue());
        Assert.False(packet.ContainsFloatingPointValue());
        Assert.False(packet.ContainsBooleanValue());
    }

    [Fact]
    public void Create_WithBooleanValue_PreservesBooleanType()
    {
        DateTimeOffset timestamp =
            new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

        TelemetryPacket<bool> packet =
            TelemetryPacket<bool>.Create(
                Guid.NewGuid(),
                sequenceNumber: 3,
                value: true,
                unit: "state",
                timestamp,
                timestamp);

        Assert.True(packet.Value);
        Assert.Equal("Boolean", packet.ValueTypeName);
        Assert.True(packet.ContainsBooleanValue());
        Assert.False(packet.ContainsFloatingPointValue());
        Assert.False(packet.ContainsIntegerValue());
    }

    [Fact]
    public void Create_WithUnsupportedType_ThrowsNotSupportedException()
    {
        DateTimeOffset timestamp =
            new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

        NotSupportedException exception =
            Assert.Throws<NotSupportedException>(
                () => TelemetryPacket<double>.Create(
                    Guid.NewGuid(),
                    sequenceNumber: 4,
                    value: 44.8d,
                    unit: "units",
                    timestamp,
                    timestamp));

        Assert.Contains(
            "not supported",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithFutureCapturedTimestamp_ThrowsException()
    {
        DateTimeOffset receivedAt =
            new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

        DateTimeOffset capturedAt =
            receivedAt.AddSeconds(1);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => TelemetryPacket<float>.Create(
                Guid.NewGuid(),
                sequenceNumber: 5,
                value: 25.2f,
                unit: "°C",
                capturedAt,
                receivedAt));
    }

    [Fact]
    public void UpdateHealthState_WithAnomaly_ChangesPacketState()
    {
        DateTimeOffset timestamp =
            new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

        TelemetryPacket<float> packet =
            TelemetryPacket<float>.Create(
                Guid.NewGuid(),
                sequenceNumber: 6,
                value: 79.5f,
                unit: "°C",
                timestamp,
                timestamp);

        packet.UpdateHealthState(
            TelemetryHealthState.Anomaly);

        Assert.Equal(
            TelemetryHealthState.Anomaly,
            packet.HealthState);
    }
}