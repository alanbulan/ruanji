namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// Represents a recorded operation for rollback support.
/// </summary>
public record OperationRecord
{
    /// <summary>
    /// Gets the unique identifier for this operation.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the type of operation.
    /// </summary>
    public required OperationType Type { get; init; }

    /// <summary>
    /// Gets the description of the operation.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the start time of the operation.
    /// </summary>
    public required DateTime StartTime { get; init; }

    /// <summary>
    /// Gets the end time of the operation.
    /// </summary>
    public DateTime? EndTime { get; init; }

    /// <summary>
    /// Gets whether the operation was successful.
    /// </summary>
    public bool? Success { get; init; }

    /// <summary>
    /// Gets the list of actions performed during this operation.
    /// </summary>
    public IReadOnlyList<OperationAction> Actions { get; init; } = Array.Empty<OperationAction>();
}

/// <summary>
/// Types of operations that can be logged.
/// </summary>
public enum OperationType
{
    /// <summary>Software migration operation</summary>
    Migration,
    /// <summary>Cleanup operation</summary>
    Cleanup,
    /// <summary>Registry update operation</summary>
    RegistryUpdate,
    /// <summary>Rollback operation</summary>
    Rollback
}

/// <summary>
/// Represents a single action within an operation.
/// </summary>
public record OperationAction
{
    /// <summary>
    /// Gets the type of action.
    /// </summary>
    public required string ActionType { get; init; }

    /// <summary>
    /// Gets the description of the action.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the timestamp of the action.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the original value before the action (for rollback).
    /// </summary>
    public string? OriginalValue { get; init; }

    /// <summary>
    /// Gets the new value after the action.
    /// </summary>
    public string? NewValue { get; init; }

    /// <summary>
    /// Gets whether this action can be rolled back.
    /// </summary>
    public bool CanRollback { get; init; }
}
