using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// Coordinates all software organization operations.
/// </summary>
public class SoftwareOrganizerService
{
    private readonly ISoftwareScanner _scanner;
    private readonly ISoftwareClassifier _classifier;
    private readonly IMigrationEngine _migrationEngine;
    private readonly ICleanupEngine _cleanupEngine;
    private readonly IOperationLogger _logger;
    private readonly IConfigurationService _configService;

    public SoftwareOrganizerService(
        ISoftwareScanner scanner,
        ISoftwareClassifier classifier,
        IMigrationEngine migrationEngine,
        ICleanupEngine cleanupEngine,
        IOperationLogger logger,
        IConfigurationService configService)
    {
        _scanner = scanner;
        _classifier = classifier;
        _migrationEngine = migrationEngine;
        _cleanupEngine = cleanupEngine;
        _logger = logger;
        _configService = configService;
    }

    /// <summary>
    /// Scans and classifies all installed software.
    /// </summary>
    public async Task<IReadOnlyList<SoftwareEntry>> ScanAndClassifyAsync(
        CancellationToken cancellationToken = default)
    {
        // Scan for software
        var entries = await _scanner.ScanInstalledSoftwareAsync(cancellationToken);

        // Classify each entry
        var classifiedEntries = new List<SoftwareEntry>();
        foreach (var entry in entries)
        {
            var category = _classifier.Classify(entry);
            var classifiedEntry = entry with { Category = category };
            classifiedEntries.Add(classifiedEntry);
        }

        return classifiedEntries;
    }

    /// <summary>
    /// Performs a complete migration workflow.
    /// </summary>
    public async Task<MigrationResult> MigrateAsync(
        SoftwareEntry entry,
        string targetPath,
        NamingTemplate template,
        MigrationOptions options,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Begin operation logging
        var operationId = await _logger.BeginOperationAsync(
            OperationType.Migration,
            $"Migrating {entry.Name} to {targetPath}");

        try
        {
            // Create migration plan
            var plan = await _migrationEngine.CreatePlanAsync(entry, targetPath, template);

            // Check space
            if (plan.TotalSizeBytes > plan.AvailableSpaceBytes)
            {
                await _logger.CompleteOperationAsync(operationId, false);
                return new MigrationResult
                {
                    Success = false,
                    ErrorMessage = "Insufficient disk space"
                };
            }

            // Execute migration
            var result = await _migrationEngine.ExecuteAsync(plan, options, progress, cancellationToken);

            // Log completion
            await _logger.CompleteOperationAsync(operationId, result.Success);

            return result;
        }
        catch (Exception ex)
        {
            await _logger.CompleteOperationAsync(operationId, false);
            return new MigrationResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Performs a complete cleanup workflow.
    /// </summary>
    public async Task<CleanupResult> CleanupAsync(
        IEnumerable<CleanupItem> items,
        bool moveToRecycleBin = true,
        IProgress<CleanupProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Begin operation logging
        var operationId = await _logger.BeginOperationAsync(
            OperationType.Cleanup,
            $"Cleaning up {items.Count()} items");

        try
        {
            var result = await _cleanupEngine.CleanupAsync(
                items,
                moveToRecycleBin,
                progress,
                cancellationToken);

            await _logger.CompleteOperationAsync(operationId, result.Success);
            return result;
        }
        catch (Exception ex)
        {
            await _logger.CompleteOperationAsync(operationId, false);
            return new CleanupResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Rolls back a previous operation.
    /// </summary>
    public async Task<RollbackResult> RollbackAsync(
        string operationId,
        CancellationToken cancellationToken = default)
    {
        var operation = await _logger.GetOperationAsync(operationId);
        if (operation == null)
        {
            return new RollbackResult
            {
                Success = false,
                ErrorMessage = "Operation not found"
            };
        }

        // Begin rollback logging
        var rollbackId = await _logger.BeginOperationAsync(
            OperationType.Rollback,
            $"Rolling back operation {operationId}");

        try
        {
            var result = await _migrationEngine.RollbackAsync(operationId, cancellationToken);
            await _logger.CompleteOperationAsync(rollbackId, result.Success);
            return result;
        }
        catch (Exception ex)
        {
            await _logger.CompleteOperationAsync(rollbackId, false);
            return new RollbackResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
