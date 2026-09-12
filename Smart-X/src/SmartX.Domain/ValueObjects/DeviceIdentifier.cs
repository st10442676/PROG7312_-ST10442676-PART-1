using System.Text.RegularExpressions;

namespace SmartX.Domain.ValueObjects;

/// <summary>
/// Represents a validated MAC address or unique Smart-X device identifier.
/// </summary>
public sealed record DeviceIdentifier
{
    private const int MinimumLength = 3;
    private const int MaximumLength = 64;

    private static readonly Regex MacAddressPattern = new(
        @"^(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$",
        RegexOptions.Compiled |
        RegexOptions.CultureInvariant |
        RegexOptions.NonBacktracking);

    private static readonly Regex UniqueIdentifierPattern = new(
        @"^[A-Za-z0-9][A-Za-z0-9._:-]*$",
        RegexOptions.Compiled |
        RegexOptions.CultureInvariant |
        RegexOptions.NonBacktracking);

    private DeviceIdentifier(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the normalised device identifier.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a validated device identifier.
    /// </summary>
    /// <param name="candidate">
    /// A MAC address or custom unique identifier.
    /// </param>
    /// <returns>A validated device identifier.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the supplied identifier is invalid.
    /// </exception>
    public static DeviceIdentifier Create(string candidate)
    {
        if (!TryCreate(candidate, out DeviceIdentifier? identifier, out string error))
        {
            throw new ArgumentException(error, nameof(candidate));
        }

        return identifier!;
    }

    /// <summary>
    /// Attempts to create a device identifier without throwing an exception.
    /// </summary>
    public static bool TryCreate(
        string? candidate,
        out DeviceIdentifier? identifier,
        out string error)
    {
        identifier = null;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            error = "A device MAC address or unique identifier is required.";
            return false;
        }

        string trimmedCandidate = candidate.Trim();

        if (trimmedCandidate.Length is < MinimumLength or > MaximumLength)
        {
            error =
                $"The device identifier must contain between " +
                $"{MinimumLength} and {MaximumLength} characters.";

            return false;
        }

        bool isMacAddress = MacAddressPattern.IsMatch(trimmedCandidate);
        bool isUniqueIdentifier = UniqueIdentifierPattern.IsMatch(trimmedCandidate);

        if (!isMacAddress && !isUniqueIdentifier)
        {
            error =
                "The device identifier may only contain letters, numbers, " +
                "periods, underscores, colons or hyphens.";

            return false;
        }

        string normalisedValue = isMacAddress
            ? trimmedCandidate.Replace('-', ':').ToUpperInvariant()
            : trimmedCandidate;

        identifier = new DeviceIdentifier(normalisedValue);
        error = string.Empty;

        return true;
    }

    public override string ToString()
    {
        return Value;
    }
}