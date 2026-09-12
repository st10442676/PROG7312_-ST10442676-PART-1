namespace SmartX.Domain.ValueObjects;

/// <summary>
/// Represents the hierarchical location of a deployed Smart-X sensor.
/// </summary>
public sealed record DeploymentLocation
{
    private const int MaximumPartLength = 80;

    private DeploymentLocation(
        string facility,
        string zone,
        string? subZone,
        string nodeId)
    {
        Facility = facility;
        Zone = zone;
        SubZone = subZone;
        NodeId = nodeId;
    }

    /// <summary>
    /// Gets the highest-level deployment facility.
    /// </summary>
    public string Facility { get; }

    /// <summary>
    /// Gets the zone within the facility.
    /// </summary>
    public string Zone { get; }

    /// <summary>
    /// Gets the optional sub-zone within the zone.
    /// </summary>
    public string? SubZone { get; }

    /// <summary>
    /// Gets the room, node or final deployment position.
    /// </summary>
    public string NodeId { get; }

    /// <summary>
    /// Gets the complete human-readable deployment path.
    /// </summary>
    public string Path
    {
        get
        {
            List<string> parts = [Facility, Zone];

            if (!string.IsNullOrWhiteSpace(SubZone))
            {
                parts.Add(SubZone);
            }

            parts.Add(NodeId);

            return string.Join(" / ", parts);
        }
    }

    /// <summary>
    /// Creates a validated hierarchical deployment location.
    /// </summary>
    public static DeploymentLocation Create(
        string facility,
        string zone,
        string? subZone,
        string nodeId)
    {
        if (!TryCreate(
            facility,
            zone,
            subZone,
            nodeId,
            out DeploymentLocation? location,
            out string error))
        {
            throw new ArgumentException(error);
        }

        return location!;
    }

    /// <summary>
    /// Attempts to create a deployment location without throwing an exception.
    /// </summary>
    public static bool TryCreate(
        string? facility,
        string? zone,
        string? subZone,
        string? nodeId,
        out DeploymentLocation? location,
        out string error)
    {
        location = null;

        string normalisedFacility = Normalise(facility);
        string normalisedZone = Normalise(zone);
        string normalisedSubZone = Normalise(subZone);
        string normalisedNodeId = Normalise(nodeId);

        if (!IsRequiredPartValid(normalisedFacility))
        {
            error =
                $"Facility is required and may not exceed " +
                $"{MaximumPartLength} characters.";

            return false;
        }

        if (!IsRequiredPartValid(normalisedZone))
        {
            error =
                $"Zone is required and may not exceed " +
                $"{MaximumPartLength} characters.";

            return false;
        }

        if (normalisedSubZone.Length > MaximumPartLength)
        {
            error =
                $"Sub-zone may not exceed {MaximumPartLength} characters.";

            return false;
        }

        if (!IsRequiredPartValid(normalisedNodeId))
        {
            error =
                $"A room or node identifier is required and may not exceed " +
                $"{MaximumPartLength} characters.";

            return false;
        }

        location = new DeploymentLocation(
            normalisedFacility,
            normalisedZone,
            string.IsNullOrWhiteSpace(normalisedSubZone)
                ? null
                : normalisedSubZone,
            normalisedNodeId);

        error = string.Empty;

        return true;
    }

    public override string ToString()
    {
        return Path;
    }

    private static bool IsRequiredPartValid(string value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Length <= MaximumPartLength;
    }

    private static string Normalise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(
            " ",
            value.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries));
    }
}