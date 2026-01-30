namespace WindowsSoftwareOrganizer.Core.Models;

/// <summary>
/// 大小分析结果。
/// </summary>
public record SizeAnalysisResult
{
    public string RootPath { get; init; } = string.Empty;
    public long TotalSize { get; init; }
    public IReadOnlyList<SizeAnalysisItem> Items { get; init; } = Array.Empty<SizeAnalysisItem>();
    public int TotalFiles { get; init; }
    public int TotalDirectories { get; init; }
    public TimeSpan AnalysisDuration { get; init; }
}

/// <summary>
/// 大小分析项。
/// </summary>
public record SizeAnalysisItem
{
    public string Name { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public long Size { get; init; }
    public double Percentage { get; init; }
    public bool IsDirectory { get; init; }
    public IReadOnlyList<SizeAnalysisItem>? Children { get; init; }
}

/// <summary>
/// 大小分析进度。
/// </summary>
public record SizeAnalysisProgress
{
    public int ProgressPercentage { get; init; }
    public string? CurrentPath { get; init; }
    public int FilesScanned { get; init; }
    public int DirectoriesScanned { get; init; }
    public long BytesScanned { get; init; }
}
