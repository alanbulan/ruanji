namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// Represents a naming template for directory naming.
/// </summary>
public record NamingTemplate
{
    /// <summary>
    /// Gets the unique identifier for this template.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display name of the template.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the pattern string with placeholders.
    /// Supported variables: {Category}, {Name}, {Version}, {Vendor}, {Date}
    /// </summary>
    public required string Pattern { get; init; }

    /// <summary>
    /// Gets the description of the template.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets whether this is a preset (built-in) template.
    /// </summary>
    public bool IsPreset { get; init; }
}

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Gets whether the validation passed.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    public static ValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors
    };
}
