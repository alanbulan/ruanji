namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// Represents a registry reference to a path.
/// </summary>
public record RegistryReference
{
    /// <summary>
    /// Gets the registry key path.
    /// </summary>
    public required string KeyPath { get; init; }

    /// <summary>
    /// Gets the value name within the key.
    /// </summary>
    public required string ValueName { get; init; }

    /// <summary>
    /// Gets the current value data.
    /// </summary>
    public required string ValueData { get; init; }

    /// <summary>
    /// Gets the registry value type.
    /// </summary>
    public required RegistryValueType ValueType { get; init; }
}

/// <summary>
/// Types of registry values.
/// </summary>
public enum RegistryValueType
{
    /// <summary>String value (REG_SZ)</summary>
    String,
    /// <summary>Expandable string value (REG_EXPAND_SZ)</summary>
    ExpandString,
    /// <summary>Multi-string value (REG_MULTI_SZ)</summary>
    MultiString,
    /// <summary>Binary value (REG_BINARY)</summary>
    Binary,
    /// <summary>DWORD value (REG_DWORD)</summary>
    DWord,
    /// <summary>QWORD value (REG_QWORD)</summary>
    QWord
}

/// <summary>
/// Represents the result of a registry update operation.
/// </summary>
public record RegistryUpdateResult
{
    /// <summary>
    /// Gets whether the update was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the update failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the number of references successfully updated.
    /// </summary>
    public int UpdatedCount { get; init; }

    /// <summary>
    /// Gets the number of references that failed to update.
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// Gets the backup ID for this update.
    /// </summary>
    public string? BackupId { get; init; }
}

/// <summary>
/// Represents a report of registry updates.
/// </summary>
public record RegistryUpdateReport
{
    /// <summary>
    /// Gets the operation ID this report is for.
    /// </summary>
    public required string OperationId { get; init; }

    /// <summary>
    /// Gets the timestamp of the report.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the list of updated registry entries.
    /// </summary>
    public IReadOnlyList<RegistryUpdateEntry> Entries { get; init; } = Array.Empty<RegistryUpdateEntry>();

    /// <summary>
    /// Gets the total number of entries updated.
    /// </summary>
    public int TotalUpdated { get; init; }

    /// <summary>
    /// Gets the total number of entries that failed.
    /// </summary>
    public int TotalFailed { get; init; }
}

/// <summary>
/// Represents a single registry update entry in a report.
/// </summary>
public record RegistryUpdateEntry
{
    /// <summary>
    /// Gets the registry key path.
    /// </summary>
    public required string KeyPath { get; init; }

    /// <summary>
    /// Gets the value name.
    /// </summary>
    public required string ValueName { get; init; }

    /// <summary>
    /// Gets the old value.
    /// </summary>
    public required string OldValue { get; init; }

    /// <summary>
    /// Gets the new value.
    /// </summary>
    public required string NewValue { get; init; }

    /// <summary>
    /// Gets whether the update was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the update failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
