namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// 文件搜索条件。
/// </summary>
public record FileSearchCriteria
{
    public required string Pattern { get; init; }
    public bool UseWildcard { get; init; } = true;
    public bool IncludeSubdirectories { get; init; } = true;
    public IReadOnlyList<string>? Extensions { get; init; }
    public long? MinSize { get; init; }
    public long? MaxSize { get; init; }
    public DateTime? ModifiedAfter { get; init; }
    public DateTime? ModifiedBefore { get; init; }
    public bool MatchCase { get; init; } = false;
}
