namespace SmartX.Domain.Enums;

/// <summary>
/// Defines the supported levels in a Smart-X deployment hierarchy.
/// </summary>
public enum DeploymentNodeType
{
    /// <summary>
    /// The highest-level physical deployment site.
    /// </summary>
    Facility = 1,

    /// <summary>
    /// A logical or physical zone within a facility.
    ///.
    /// </summary>
    Zone = 2,

    /// <summary>
    /// A smaller operational area within a zone.
    /// </summary>
    SubZone = 3,

    /// <summary>
    /// A room or enclosed area containing sensor nodes.
    /// </summary>
    Room = 4,

    /// <summary>
    /// The final physical sensor position.
    /// </summary>
    Node = 5
}