using System.Collections.ObjectModel;
using SmartX.Domain.Enums;

namespace SmartX.Domain.Deployment;

/// <summary>
/// Represents one node in a nested Smart-X deployment tree.
/// </summary>
public sealed class DeploymentNode
{
    private const int MaximumNameLength = 100;

    private readonly List<DeploymentNode> _children = [];

    private readonly ReadOnlyCollection<DeploymentNode> _readOnlyChildren;

    /// <summary>
    /// Creates a validated deployment node.
    /// </summary>
    public DeploymentNode(
        string name,
        DeploymentNodeType nodeType,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "A deployment node name is required.",
                nameof(name));
        }

        string normalisedName = string.Join(
            " ",
            name.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries));

        if (normalisedName.Length > MaximumNameLength)
        {
            throw new ArgumentException(
                $"A deployment node name cannot exceed " +
                $"{MaximumNameLength} characters.",
                nameof(name));
        }

        if (!Enum.IsDefined(nodeType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(nodeType),
                nodeType,
                "The deployment node type is not supported.");
        }

        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "A deployment node identifier cannot be empty.",
                nameof(id));
        }

        Id = id ?? Guid.NewGuid();
        Name = normalisedName;
        NodeType = nodeType;

        _readOnlyChildren = _children.AsReadOnly();
    }

    /// <summary>
    /// Gets the unique identifier of this deployment node.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the human-readable node name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the node's level in the deployment hierarchy.
    /// </summary>
    public DeploymentNodeType NodeType { get; }

    /// <summary>
    /// Gets a read-only view of the child deployment nodes.
    /// </summary>
    public IReadOnlyList<DeploymentNode> Children =>
        _readOnlyChildren;

    /// <summary>
    /// Adds a child to this deployment node.
    /// </summary>
    public void AddChild(DeploymentNode child)
    {
        ArgumentNullException.ThrowIfNull(child);

        if (ReferenceEquals(this, child))
        {
            throw new InvalidOperationException(
                "A deployment node cannot be its own direct child.");
        }

        if (_children.Any(existingChild =>
                existingChild.Id == child.Id))
        {
            throw new InvalidOperationException(
                $"Deployment node '{child.Name}' is already a direct child.");
        }

        _children.Add(child);
    }

    /// <summary>
    /// Removes a direct child from this node.
    /// </summary>
    public bool RemoveChild(Guid childId)
    {
        DeploymentNode? matchingChild =
            _children.Find(child => child.Id == childId);

        return matchingChild is not null &&
               _children.Remove(matchingChild);
    }
}