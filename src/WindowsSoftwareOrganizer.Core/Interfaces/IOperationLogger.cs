using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// Interface for logging and tracking operations for rollback support.
/// </summary>
public interface IOperationLogger
{
    /// <summary>
    /// Begins a new operation and returns its identifier.
    /// </summary>
    /// <param name="type">The type of operation.</param>
    /// <param name="description">A description of the operation.</param>
    /// <returns>The operation identifier.</returns>
    Task<string> BeginOperationAsync(OperationType type, string description);

    /// <summary>
    /// Logs an action within an operation.
    /// </summary>
    /// <param name="operationId">The operation identifier.</param>
    /// <param name="action">The action to log.</param>
    Task LogActionAsync(string operationId, OperationAction action);

    /// <summary>
    /// Marks an operation as complete.
    /// </summary>
    /// <param name="operationId">The operation identifier.</param>
    /// <param name="success">Whether the operation was successful.</param>
    Task CompleteOperationAsync(string operationId, bool success);

    /// <summary>
    /// Gets the operation history.
    /// </summary>
    /// <param name="since">Optional start date filter.</param>
    /// <param name="limit">Optional maximum number of records to return.</param>
    /// <returns>A list of operation records.</returns>
    Task<IReadOnlyList<OperationRecord>> GetHistoryAsync(
        DateTime? since = null,
        int? limit = null);

    /// <summary>
    /// Gets a specific operation by its identifier.
    /// </summary>
    /// <param name="operationId">The operation identifier.</param>
    /// <returns>The operation record, or null if not found.</returns>
    Task<OperationRecord?> GetOperationAsync(string operationId);
}
