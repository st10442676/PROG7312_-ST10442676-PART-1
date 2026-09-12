using SmartX.Domain.Enums;

namespace SmartX.Domain.Deployment;

/// <summary>
/// Recursively validates nested Smart-X deployment trees.
/// </summary>
public sealed class DeploymentTreeValidator
{
    private const int DefaultMaximumDepth = 64;

    private readonly int _maximumAllowedDepth;

    /// <summary>
    /// Creates a recursive deployment-tree validator.
    /// </summary>
    public DeploymentTreeValidator(
        int maximumAllowedDepth = DefaultMaximumDepth)
    {
        if (maximumAllowedDepth <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumAllowedDepth),
                "Maximum recursion depth must be greater than zero.");
        }

        _maximumAllowedDepth = maximumAllowedDepth;
    }

    /// <summary>
    /// Validates an entire deployment tree from its facility root.
    /// </summary>
    public DeploymentValidationResult Validate(
        DeploymentNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        List<string> errors = [];

        HashSet<Guid> activeRecursionPath = [];

        HashSet<Guid> visitedNodes = [];

        List<string> displayPath = [];

        int nodesValidated = 0;
        int maximumDepthReached = 0;

        if (root.NodeType != DeploymentNodeType.Facility)
        {
            errors.Add(
                $"The deployment root must be a Facility, but " +
                $"'{root.Name}' is configured as {root.NodeType}.");
        }

        ValidateNodeRecursively(
            root,
            parent: null,
            depth: 1,
            errors,
            activeRecursionPath,
            visitedNodes,
            displayPath,
            ref nodesValidated,
            ref maximumDepthReached);

        return new DeploymentValidationResult(
            errors.AsReadOnly(),
            nodesValidated,
            maximumDepthReached);
    }

    private void ValidateNodeRecursively(
        DeploymentNode node,
        DeploymentNode? parent,
        int depth,
        List<string> errors,
        HashSet<Guid> activeRecursionPath,
        HashSet<Guid> visitedNodes,
        List<string> displayPath,
        ref int nodesValidated,
        ref int maximumDepthReached)
    {
        displayPath.Add(
            $"{node.NodeType}: {node.Name}");

        try
        {
            string currentPath =
                string.Join(" -> ", displayPath);

            if (depth > _maximumAllowedDepth)
            {
                errors.Add(
                    $"Maximum deployment depth of " +
                    $"{_maximumAllowedDepth} was exceeded at " +
                    $"'{currentPath}'.");

                return;
            }

            maximumDepthReached =
                Math.Max(maximumDepthReached, depth);

            if (!activeRecursionPath.Add(node.Id))
            {
                errors.Add(
                    $"A circular deployment reference was detected at " +
                    $"'{currentPath}'.");

                return;
            }

            try
            {
                if (!visitedNodes.Add(node.Id))
                {
                    errors.Add(
                        $"Deployment node '{node.Name}' appears more than " +
                        $"once in the tree at '{currentPath}'.");

                    return;
                }

                nodesValidated++;

                if (parent is not null &&
                    !IsAllowedRelationship(
                        parent.NodeType,
                        node.NodeType))
                {
                    errors.Add(
                        $"Invalid deployment relationship at " +
                        $"'{currentPath}'. A {parent.NodeType} cannot " +
                        $"contain a {node.NodeType}.");
                }

                ValidateChildRequirements(
                    node,
                    currentPath,
                    errors);

                foreach (DeploymentNode child in node.Children)
                {
                    ValidateNodeRecursively(
                        child,
                        node,
                        depth + 1,
                        errors,
                        activeRecursionPath,
                        visitedNodes,
                        displayPath,
                        ref nodesValidated,
                        ref maximumDepthReached);
                }
            }
            finally
            {
                activeRecursionPath.Remove(node.Id);
            }
        }
        finally
        {
            displayPath.RemoveAt(displayPath.Count - 1);
        }
    }

    private static void ValidateChildRequirements(
        DeploymentNode node,
        string currentPath,
        List<string> errors)
    {
        if (node.NodeType == DeploymentNodeType.Node &&
            node.Children.Count > 0)
        {
            errors.Add(
                $"Sensor node '{currentPath}' must be a leaf and " +
                $"cannot contain child nodes.");

            return;
        }

        if (node.NodeType != DeploymentNodeType.Node &&
            node.Children.Count == 0)
        {
            errors.Add(
                $"Deployment container '{currentPath}' must contain " +
                $"at least one correctly configured child.");
        }
    }

    private static bool IsAllowedRelationship(
        DeploymentNodeType parent,
        DeploymentNodeType child)
    {
        return (parent, child) switch
        {
            (
                DeploymentNodeType.Facility,
                DeploymentNodeType.Zone
            ) => true,

            (
                DeploymentNodeType.Zone,
                DeploymentNodeType.SubZone
            ) => true,

            (
                DeploymentNodeType.Zone,
                DeploymentNodeType.Room
            ) => true,

            (
                DeploymentNodeType.Zone,
                DeploymentNodeType.Node
            ) => true,

            (
                DeploymentNodeType.SubZone,
                DeploymentNodeType.Room
            ) => true,

            (
                DeploymentNodeType.SubZone,
                DeploymentNodeType.Node
            ) => true,

            (
                DeploymentNodeType.Room,
                DeploymentNodeType.Node
            ) => true,

            _ => false
        };
    }
}