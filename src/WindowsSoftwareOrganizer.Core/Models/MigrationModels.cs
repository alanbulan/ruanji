namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// Represents a migration plan for moving software to a new location.
/// </summary>
public record MigrationPlan
{
    /// <summary>
    /// Gets the unique identifier for this plan.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the software entry being migrated.
    /// </summary>
    public required SoftwareEntry Software { get; init; }

    /// <summary>
    /// Gets the source path of the migration.
    /// </summary>
    public required string SourcePath { get; init; }

    /// <summary>
    /// Gets the target path of the migration.
    /// </summary>
    public required string TargetPath { get; init; }

    /// <summary>
    /// Gets the list of file operations to perform.
    /// </summary>
    public required IReadOnlyList<FileMoveOperation> FileOperations { get; init; }

    /// <summary>
    /// Gets the total size in bytes of files to migrate.
    /// </summary>
    public required long TotalSizeBytes { get; init; }

    /// <summary>
    /// Gets the available space in bytes at the target location.
    /// </summary>
    public required long AvailableSpaceBytes { get; init; }

    /// <summary>
    /// Gets the recommended link type for this migration.
    /// </summary>
    public LinkType RecommendedLinkType { get; init; }
}

/// <summary>
/// Represents a single file move operation.
/// </summary>
public record FileMoveOperation
{
    /// <summary>
    /// Gets the source path of the file.
    /// </summary>
    public required string SourcePath { get; init; }

    /// <summary>
    /// Gets the target path of the file.
    /// </summary>
    public required string TargetPath { get; init; }

    /// <summary>
    /// Gets the size of the file in bytes.
    /// </summary>
    public long SizeBytes { get; init; }
}

/// <summary>
/// Represents the type of link to create after migration.
/// </summary>
public enum LinkType
{
    /// <summary>NTFS Junction (directory hard link)</summary>
    Junction,
    /// <summary>Symbolic link (requires admin privileges)</summary>
    SymbolicLink
}

/// <summary>
/// Options for migration operations.
/// </summary>
public record MigrationOptions
{
    /// <summary>
    /// Gets the type of link to create after migration.
    /// </summary>
    public LinkType LinkType { get; init; } = LinkType.Junction;

    /// <summary>
    /// Gets whether to update registry references.
    /// </summary>
    public bool UpdateRegistry { get; init; } = true;

    /// <summary>
    /// Gets whether to verify file integrity after copy.
    /// </summary>
    public bool VerifyIntegrity { get; init; } = true;

    /// <summary>
    /// Gets the conflict resolution strategy.
    /// </summary>
    public ConflictResolution OnFileConflict { get; init; } = ConflictResolution.Ask;

    /// <summary>
    /// Gets the locked file handling strategy.
    /// </summary>
    public LockedFileHandling OnLockedFile { get; init; } = LockedFileHandling.Ask;
}

/// <summary>
/// Strategies for handling file conflicts.
/// </summary>
public enum ConflictResolution
{
    /// <summary>Ask the user what to do</summary>
    Ask,
    /// <summary>Skip the conflicting file</summary>
    Skip,
    /// <summary>Overwrite the existing file</summary>
    Overwrite,
    /// <summary>Rename the new file</summary>
    Rename
}

/// <summary>
/// Strategies for handling locked files.
/// </summary>
public enum LockedFileHandling
{
    /// <summary>Ask the user what to do</summary>
    Ask,
    /// <summary>Skip the locked file</summary>
    Skip,
    /// <summary>Abort the entire operation</summary>
    Abort
}

/// <summary>
/// Represents the result of a migration operation.
/// </summary>
public record MigrationResult
{
    /// <summary>
    /// Gets whether the migration was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the operation ID for this migration.
    /// </summary>
    public string? OperationId { get; init; }

    /// <summary>
    /// Gets the error message if the migration failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the list of files that were successfully migrated.
    /// </summary>
    public IReadOnlyList<string> MigratedFiles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the list of files that were skipped.
    /// </summary>
    public IReadOnlyList<string> SkippedFiles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the list of files that failed to migrate.
    /// </summary>
    public IReadOnlyList<string> FailedFiles { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Represents the result of a rollback operation.
/// </summary>
public record RollbackResult
{
    /// <summary>
    /// Gets whether the rollback was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the rollback failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the list of files that were restored.
    /// </summary>
    public IReadOnlyList<string> RestoredFiles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the list of files that failed to restore.
    /// </summary>
    public IReadOnlyList<string> FailedFiles { get; init; } = Array.Empty<string>();
}
