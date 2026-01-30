namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// 类型统计结果。
/// </summary>
public record TypeStatisticsResult
{
    public string RootPath { get; init; } = string.Empty;
    public IReadOnlyList<TypeStatisticsItem> Items { get; init; } = Array.Empty<TypeStatisticsItem>();
    public int TotalFiles { get; init; }
    public long TotalSize { get; init; }
}

/// <summary>
/// 类型统计项。
/// </summary>
public record TypeStatisticsItem
{
    public string Extension { get; init; } = string.Empty;
    public FileTypeCategory Category { get; init; }
    public int FileCount { get; init; }
    public long TotalSize { get; init; }
    public double SizePercentage { get; init; }
    public double CountPercentage { get; init; }
}
