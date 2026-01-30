namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// Represents the result of a link creation operation.
/// </summary>
public record LinkResult
{
    /// <summary>
    /// Gets whether the link was created successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the link creation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the path of the created link.
    /// </summary>
    public string? LinkPath { get; init; }

    /// <summary>
    /// Gets the target path of the link.
    /// </summary>
    public string? TargetPath { get; init; }

    /// <summary>
    /// Gets the type of link that was created.
    /// </summary>
    public LinkType LinkType { get; init; }
}

/// <summary>
/// Represents information about an existing link.
/// </summary>
public record LinkInfo
{
    /// <summary>
    /// Gets the path of the link.
    /// </summary>
    public required string LinkPath { get; init; }

    /// <summary>
    /// Gets the target path the link points to.
    /// </summary>
    public required string TargetPath { get; init; }

    /// <summary>
    /// Gets the type of link.
    /// </summary>
    public required LinkType LinkType { get; init; }

    /// <summary>
    /// Gets whether the target exists.
    /// </summary>
    public bool TargetExists { get; init; }
}
