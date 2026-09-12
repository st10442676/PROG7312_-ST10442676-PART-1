using System.Numerics;

namespace SmartX.Domain.Telemetry;

/// <summary>
/// Represents a high-performance numeric sensor reading that supports
/// direct aggregation, delta calculation and value comparison.
/// </summary>
/// <typeparam name="T">
/// A numeric value type such as int, float, double or decimal.
/// </typeparam>
public readonly record struct NumericSensorReading<T>
    where T : struct, INumber<T>
{
    private const int MaximumUnitLength = 20;

    /// <summary>
    /// Creates a validated numeric sensor reading.
    /// </summary>
    /// <param name="value">The strongly typed numeric value.</param>
    /// <param name="unit">The measurement unit.</param>
    /// <param name="capturedAtUtc">The time recorded by the sensor.</param>
    public NumericSensorReading(
        T value,
        string unit,
        DateTimeOffset capturedAtUtc)
    {
        if (!T.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                "The sensor value must be a finite number.");
        }

        Value = value;
        Unit = NormaliseUnit(unit);
        CapturedAtUtc = capturedAtUtc;
    }

    /// <summary>
    /// Gets the reading in its original numeric type.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets the unit of measurement.
    /// </summary>
    public string Unit { get; }

    /// <summary>
    /// Gets the UTC timestamp recorded by the sensor.
    /// </summary>
    public DateTimeOffset CapturedAtUtc { get; }

    /// <summary>
    /// Adds two compatible sensor readings.
    /// </summary>
    /// <remarks>
    /// This supports operations such as combining the electrical
    /// load reported by two smart meters.
    /// </remarks>
    public static NumericSensorReading<T> operator +(
        NumericSensorReading<T> left,
        NumericSensorReading<T> right)
    {
        EnsureCompatibleUnits(left, right);

        return new NumericSensorReading<T>(
            left.Value + right.Value,
            left.Unit,
            LatestTimestamp(left, right));
    }

    /// <summary>
    /// Calculates the numeric difference between compatible readings.
    /// </summary>
    /// <remarks>
    /// This can be used to calculate a change or delta between readings.
    /// </remarks>
    public static NumericSensorReading<T> operator -(
        NumericSensorReading<T> left,
        NumericSensorReading<T> right)
    {
        EnsureCompatibleUnits(left, right);

        return new NumericSensorReading<T>(
            left.Value - right.Value,
            left.Unit,
            LatestTimestamp(left, right));
    }

    /// <summary>
    /// Determines whether the left reading is greater than the right.
    /// </summary>
    public static bool operator >(
        NumericSensorReading<T> left,
        NumericSensorReading<T> right)
    {
        EnsureCompatibleUnits(left, right);
        return left.Value > right.Value;
    }

    /// <summary>
    /// Determines whether the left reading is lower than the right.
    /// </summary>
    public static bool operator <(
        NumericSensorReading<T> left,
        NumericSensorReading<T> right)
    {
        EnsureCompatibleUnits(left, right);
        return left.Value < right.Value;
    }

    /// <summary>
    /// Determines whether the left reading is greater than or equal
    /// to the right reading.
    /// </summary>
    public static bool operator >=(
        NumericSensorReading<T> left,
        NumericSensorReading<T> right)
    {
        EnsureCompatibleUnits(left, right);
        return left.Value >= right.Value;
    }

    /// <summary>
    /// Determines whether the left reading is lower than or equal
    /// to the right reading.
    /// </summary>
    public static bool operator <=(
        NumericSensorReading<T> left,
        NumericSensorReading<T> right)
    {
        EnsureCompatibleUnits(left, right);
        return left.Value <= right.Value;
    }

    /// <summary>
    /// Returns a dashboard-friendly representation of the reading.
    /// </summary>
    public override string ToString()
    {
        return $"{Value} {Unit}";
    }

    private static DateTimeOffset LatestTimestamp(
        NumericSensorReading<T> left,
        NumericSensorReading<T> right)
    {
        return left.CapturedAtUtc >= right.CapturedAtUtc
            ? left.CapturedAtUtc
            : right.CapturedAtUtc;
    }

    private static void EnsureCompatibleUnits(
        NumericSensorReading<T> left,
        NumericSensorReading<T> right)
    {
        bool unitsMatch = string.Equals(
            left.Unit,
            right.Unit,
            StringComparison.OrdinalIgnoreCase);

        if (!unitsMatch)
        {
            throw new InvalidOperationException(
                $"Cannot combine or compare '{left.Unit}' and " +
                $"'{right.Unit}' sensor readings.");
        }
    }

    private static string NormaliseUnit(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException(
                "A measurement unit is required.",
                nameof(unit));
        }

        string normalisedUnit = unit.Trim();

        if (normalisedUnit.Length > MaximumUnitLength)
        {
            throw new ArgumentException(
                $"The measurement unit cannot exceed " +
                $"{MaximumUnitLength} characters.",
                nameof(unit));
        }

        return normalisedUnit;
    }
}