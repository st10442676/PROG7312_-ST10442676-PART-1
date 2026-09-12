using SmartX.Domain.Deployment;
using SmartX.Domain.Enums;
using Xunit;

namespace SmartX.Tests.Domain;

/// <summary>
/// Verifies recursive validation of nested deployment trees.
/// </summary>
public sealed class DeploymentTreeValidatorTests
{
    [Fact]
    public void Validate_WithCorrectHierarchy_ReturnsValidResult()
    {
        DeploymentNode facility =
            new(
                "Hydroponic Facility A",
                DeploymentNodeType.Facility);

        DeploymentNode zone =
            new(
                "Zone 1",
                DeploymentNodeType.Zone);

        DeploymentNode subZone =
            new(
                "Sub-Zone B",
                DeploymentNodeType.SubZone);

        DeploymentNode sensorNode =
            new(
                "ESP32 Node 07",
                DeploymentNodeType.Node);

        facility.AddChild(zone);
        zone.AddChild(subZone);
        subZone.AddChild(sensorNode);

        DeploymentTreeValidator validator = new();

        DeploymentValidationResult result =
            validator.Validate(facility);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(4, result.NodesValidated);
        Assert.Equal(4, result.MaximumDepthReached);
    }

    [Fact]
    public void Validate_WithInvalidParentChildRelationship_ReturnsError()
    {
        DeploymentNode facility =
            new(
                "Facility A",
                DeploymentNodeType.Facility);

        DeploymentNode sensorNode =
            new(
                "Node 01",
                DeploymentNodeType.Node);

        facility.AddChild(sensorNode);

        DeploymentTreeValidator validator = new();

        DeploymentValidationResult result =
            validator.Validate(facility);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "cannot contain",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithIncompleteContainer_ReturnsError()
    {
        DeploymentNode facility =
            new(
                "Facility A",
                DeploymentNodeType.Facility);

        DeploymentTreeValidator validator = new();

        DeploymentValidationResult result =
            validator.Validate(facility);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "at least one",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WithCircularReference_DetectsCycleSafely()
    {
        DeploymentNode facility =
            new(
                "Facility A",
                DeploymentNodeType.Facility);

        DeploymentNode zone =
            new(
                "Zone 1",
                DeploymentNodeType.Zone);

        facility.AddChild(zone);
        zone.AddChild(facility);

        DeploymentTreeValidator validator = new();

        DeploymentValidationResult result =
            validator.Validate(facility);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "circular",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WhenMaximumDepthIsExceeded_ReturnsError()
    {
        DeploymentNode facility =
            new(
                "Facility A",
                DeploymentNodeType.Facility);

        DeploymentNode zone =
            new(
                "Zone 1",
                DeploymentNodeType.Zone);

        DeploymentNode sensorNode =
            new(
                "Node 01",
                DeploymentNodeType.Node);

        facility.AddChild(zone);
        zone.AddChild(sensorNode);

        DeploymentTreeValidator validator =
            new(maximumAllowedDepth: 2);

        DeploymentValidationResult result =
            validator.Validate(facility);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "depth",
                StringComparison.OrdinalIgnoreCase));
    }
}