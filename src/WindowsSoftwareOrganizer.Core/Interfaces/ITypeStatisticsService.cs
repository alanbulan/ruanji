using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// 文件类型统计服务接口 - 统计目录中的文件类型分布。
/// </summary>
public interface ITypeStatisticsService
{
    /// <summary>
    /// 统计指定目录的文件类型分布。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="recursive">是否递归子目录</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>类型统计结果</returns>
    Task<TypeStatisticsResult> AnalyzeAsync(
        string path,
        bool recursive = true,
        IProgress<TypeStatisticsProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按扩展名统计文件。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="recursive">是否递归子目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>扩展名统计结果</returns>
    Task<IReadOnlyList<ExtensionStatistics>> GetExtensionStatisticsAsync(
        string path,
        bool recursive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定类型的所有文件。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="category">文件类型分类</param>
    /// <param name="recursive">是否递归子目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件列表</returns>
    Task<IReadOnlyList<FileEntry>> GetFilesByCategoryAsync(
        string path,
        FileTypeCategory category,
        bool recursive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定扩展名的所有文件。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="extension">文件扩展名</param>
    /// <param name="recursive">是否递归子目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件列表</returns>
    Task<IReadOnlyList<FileEntry>> GetFilesByExtensionAsync(
        string path,
        string extension,
        bool recursive = true,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 类型统计进度。
/// </summary>
public record TypeStatisticsProgress
{
    /// <summary>
    /// 当前处理的目录。
    /// </summary>
    public string CurrentDirectory { get; init; } = string.Empty;

    /// <summary>
    /// 已处理的文件数。
    /// </summary>
    public int ProcessedFiles { get; init; }

    /// <summary>
    /// 已处理的目录数。
    /// </summary>
    public int ProcessedDirectories { get; init; }

    /// <summary>
    /// 已处理的总大小。
    /// </summary>
    public long ProcessedSize { get; init; }
}

/// <summary>
/// 扩展名统计。
/// </summary>
public record ExtensionStatistics
{
    /// <summary>
    /// 扩展名。
    /// </summary>
    public required string Extension { get; init; }

    /// <summary>
    /// 文件类型分类。
    /// </summary>
    public FileTypeCategory Category { get; init; }

    /// <summary>
    /// 文件数量。
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// 总大小。
    /// </summary>
    public long TotalSize { get; init; }

    /// <summary>
    /// 平均大小。
    /// </summary>
    public long AverageSize => Count > 0 ? TotalSize / Count : 0;

    /// <summary>
    /// 最大文件大小。
    /// </summary>
    public long MaxSize { get; init; }

    /// <summary>
    /// 最小文件大小。
    /// </summary>
    public long MinSize { get; init; }
}
