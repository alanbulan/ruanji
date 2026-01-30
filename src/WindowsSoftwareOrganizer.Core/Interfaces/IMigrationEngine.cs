using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// Interface for managing software migration operations.
/// </summary>
public interface IMigrationEngine
{
    /// <summary>
    /// Creates a migration plan for a software entry.
    /// </summary>
    /// <param name="entry">The software entry to migrate.</param>
    /// <param name="targetBasePath">The target base path for migration.</param>
    /// <param name="template">The naming template to use.</param>
    /// <returns>The migration plan.</returns>
    Task<MigrationPlan> CreatePlanAsync(
        SoftwareEntry entry,
        string targetBasePath,
        NamingTemplate template);

    /// <summary>
    /// Executes a migration plan.
    /// </summary>
    /// <param name="plan">The migration plan to execute.</param>
    /// <param name="options">Migration options.</param>
    /// <param name="progress">Progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The migration result.</returns>
    Task<MigrationResult> ExecuteAsync(
        MigrationPlan plan,
        MigrationOptions options,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a migration operation.
    /// </summary>
    /// <param name="operationId">The operation ID to roll back.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rollback result.</returns>
    Task<RollbackResult> RollbackAsync(
        string operationId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the progress of a migration operation.
/// </summary>
public class MigrationProgress
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
    /// Gets the current file being processed.
    /// </summary>
    public string? CurrentFile { get; init; }

    /// <summary>
    /// Gets the bytes transferred so far.
    /// </summary>
    public long BytesTransferred { get; init; }

    /// <summary>
    /// Gets the total bytes to transfer.
    /// </summary>
    public long TotalBytes { get; init; }
}
