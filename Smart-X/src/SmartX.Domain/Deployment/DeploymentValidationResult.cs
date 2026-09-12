namespace SmartX.Domain.Deployment;

/// <summary>
/// Contains the complete result of recursively validating
/// a Smart-X deployment tree.
/// </summary>
public sealed record DeploymentValidationResult
{
    /// <summary>
    /// Creates a deployment validation result.
    /// </summary>
    public DeploymentValidationResult(
        IReadOnlyList<string> errors,
        int nodesValidated,
        int maximumDepthReached)
    {
        ArgumentNullException.ThrowIfNull(errors);

        Errors = errors;
        NodesValidated = nodesValidated;
        MaximumDepthReached = maximumDepthReached;
    }

    /// <summary>
    /// Gets a value indicating whether the entire tree is valid.
    /// </summary>
    public bool IsValid =>
        Errors.Count == 0;

    /// <summary>
    /// Gets all validation errors discovered during recursion.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Gets the number of unique deployment nodes inspected.
    /// </summary>
    public int NodesValidated { get; }

    /// <summary>
    /// Gets the deepest level reached by the recursive algorithm.
    /// </summary>
    public int MaximumDepthReached { get; }
}