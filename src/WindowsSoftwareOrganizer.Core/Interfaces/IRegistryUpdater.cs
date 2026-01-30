using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// Interface for updating registry path references after migration.
/// </summary>
public interface IRegistryUpdater
{
    /// <summary>
    /// Finds all registry references to a specific path.
    /// </summary>
    /// <param name="oldPath">The path to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of registry references.</returns>
    Task<IReadOnlyList<RegistryReference>> FindReferencesAsync(
        string oldPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a backup of the specified registry references.
    /// </summary>
    /// <param name="references">The references to back up.</param>
    /// <returns>The backup identifier.</returns>
    Task<string> CreateBackupAsync(IEnumerable<RegistryReference> references);

    /// <summary>
    /// Updates registry references from old path to new path.
    /// </summary>
    /// <param name="references">The references to update.</param>
    /// <param name="oldPath">The old path to replace.</param>
    /// <param name="newPath">The new path to use.</param>
    /// <returns>The update result.</returns>
    Task<RegistryUpdateResult> UpdateReferencesAsync(
        IEnumerable<RegistryReference> references,
        string oldPath,
        string newPath);

    /// <summary>
    /// Restores a registry backup.
    /// </summary>
    /// <param name="backupId">The backup identifier.</param>
    Task RestoreBackupAsync(string backupId);

    /// <summary>
    /// Generates a report of registry updates for an operation.
    /// </summary>
    /// <param name="operationId">The operation identifier.</param>
    /// <returns>The update report.</returns>
    Task<RegistryUpdateReport> GenerateReportAsync(string operationId);
}
