namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// Represents an item that can be cleaned up.
/// </summary>
public record CleanupItem
{
    /// <summary>
    /// Gets the unique identifier for this cleanup item.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the path of the item to clean up.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the type of cleanup item.
    /// </summary>
    public required CleanupItemType Type { get; init; }

    /// <summary>
    /// Gets the risk level of cleaning this item.
    /// </summary>
    public required RiskLevel Risk { get; init; }

    /// <summary>
    /// Gets the size of the item in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Gets a description of the cleanup item.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the name of the related software, if any.
    /// </summary>
    public string? RelatedSoftware { get; init; }
}

/// <summary>
/// Types of cleanup items.
/// </summary>
public enum CleanupItemType
{
    /// <summary>Directory from uninstalled software</summary>
    OrphanedDirectory,
    /// <summary>Registry key from uninstalled software</summary>
    OrphanedRegistryKey,
    /// <summary>Cache directory</summary>
    CacheDirectory,
    /// <summary>Temporary file</summary>
    TempFile,
    /// <summary>Log file</summary>
    LogFile
}

/// <summary>
/// Risk levels for cleanup operations.
/// </summary>
public enum RiskLevel
{
    /// <summary>Safe to delete</summary>
    Safe,
    /// <summary>Use caution when deleting</summary>
    Caution,
    /// <summary>Potentially dangerous to delete</summary>
    Dangerous
}

/// <summary>
/// Represents the result of a cleanup operation.
/// </summary>
public record CleanupResult
{
    /// <summary>
    /// Gets whether the cleanup was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the cleanup failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the total bytes freed by the cleanup.
    /// </summary>
    public long BytesFreed { get; init; }

    /// <summary>
    /// Gets the number of items successfully cleaned.
    /// </summary>
    public int ItemsCleaned { get; init; }

    /// <summary>
    /// Gets the number of items that failed to clean.
    /// </summary>
    public int ItemsFailed { get; init; }

    /// <summary>
    /// Gets the list of items that failed to clean with their error messages.
    /// </summary>
    public IReadOnlyList<CleanupFailure> Failures { get; init; } = Array.Empty<CleanupFailure>();
}

/// <summary>
/// Represents a failed cleanup item.
/// </summary>
public record CleanupFailure
{
    /// <summary>
    /// Gets the path of the item that failed to clean.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string ErrorMessage { get; init; }
}
