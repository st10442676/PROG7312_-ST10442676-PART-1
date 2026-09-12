namespace SmartX.Domain.Enums;

/// <summary>
/// Defines the evidence that can be attached to a sensor profile.
/// </summary>
public enum AttachmentCategory
{
    /// <summary>
    /// A physical device configuration file.
    /// </summary>
    Configuration = 1,

    /// <summary>
    /// A photograph showing the deployed hardware.
    /// </summary>
    DeploymentPhoto = 2,

    /// <summary>
    /// A hardware or diagnostic log file.
    /// </summary>
    HardwareLog = 3
}