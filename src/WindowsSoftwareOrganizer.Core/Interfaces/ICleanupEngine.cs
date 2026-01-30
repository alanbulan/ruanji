using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// Interface for cleaning up orphaned files and cache directories.
/// </summary>
public interface ICleanupEngine
{
    /// <summary>
    /// Scans for orphaned items (directories and registry keys from uninstalled software).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of orphaned cleanup items.</returns>
    Task<IReadOnlyList<CleanupItem>> ScanOrphanedItemsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans for cache directories and temporary files.
    /// </summary>
    /// <param name="entry">Optional software entry to scan cache for. If null, scans all known caches.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of cache cleanup items.</returns>
    Task<IReadOnlyList<CleanupItem>> ScanCacheAsync(
        SoftwareEntry? entry = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs cleanup of the specified items.
    /// </summary>
    /// <param name="items">The items to clean up.</param>
    /// <param name="moveToRecycleBin">If true, moves items to recycle bin instead of permanent deletion.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cleanup result.</returns>
    Task<CleanupResult> CleanupAsync(
        IEnumerable<CleanupItem> items,
        bool moveToRecycleBin = true,
        IProgress<CleanupProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the progress of a cleanup operation.
/// </summary>
public class CleanupProgress
{
    /// <summary>
    /// Gets the current progress percentage (0-100).
    /// </summary>
    public int ProgressPercentage { get; init; }

    /// <summary>
    /// Gets the current status message.
    /// </summary>
    public string? StatusMessage { get; init; }

    /// <summary>
    /// Gets the current item being processed.
    /// </summary>
    public string? CurrentItem { get; init; }

    /// <summary>
    /// Gets the number of items processed so far.
    /// </summary>
    public int ItemsProcessed { get; init; }

    /// <summary>
    /// Gets the total number of items to process.
    /// </summary>
    public int TotalItems { get; init; }
}
