namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// 重命名操作。
/// </summary>
public record RenameOperation
{
    public required string SourcePath { get; init; }
    public required string NewName { get; init; }
}

/// <summary>
/// 批量操作结果。
/// </summary>
public record BatchOperationResult
{
    public int TotalItems { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public required IReadOnlyList<BatchOperationError> Errors { get; init; }
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// 批量操作错误。
/// </summary>
public record BatchOperationError
{
    public required string Path { get; init; }
    public required string ErrorMessage { get; init; }
    public Exception? Exception { get; init; }
}

/// <summary>
/// 批量操作进度。
/// </summary>
public record BatchOperationProgress
{
    public int TotalItems { get; init; }
    public int ProcessedItems { get; init; }
    public int ProgressPercentage { get; init; }
    public string? CurrentItem { get; init; }
    public long BytesProcessed { get; init; }
    public long TotalBytes { get; init; }
}
