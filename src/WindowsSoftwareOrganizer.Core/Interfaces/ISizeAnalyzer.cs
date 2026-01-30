using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// 大小分析器接口 - 分析目录和文件的大小分布。
/// </summary>
public interface ISizeAnalyzer
{
    /// <summary>
    /// 分析指定目录的大小分布。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="depth">分析深度（0 表示仅当前目录，-1 表示无限深度）</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>大小分析结果</returns>
    Task<SizeAnalysisResult> AnalyzeAsync(
        string path,
        int depth = 1,
        IProgress<SizeAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取目录的总大小。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>总大小（字节）</returns>
    Task<long> GetTotalSizeAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查找最大的文件。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="count">返回数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最大文件列表</returns>
    Task<IReadOnlyList<FileEntry>> FindLargestFilesAsync(
        string path,
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查找最大的目录。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="count">返回数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最大目录列表（包含大小信息）</returns>
    Task<IReadOnlyList<SizeAnalysisItem>> FindLargestDirectoriesAsync(
        string path,
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查找重复文件。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>重复文件组列表</returns>
    Task<IReadOnlyList<DuplicateFileGroup>> FindDuplicateFilesAsync(
        string path,
        IProgress<SizeAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查找空目录。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>空目录列表</returns>
    Task<IReadOnlyList<DirectoryEntry>> FindEmptyDirectoriesAsync(
        string path,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查找旧文件（长时间未访问）。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="olderThan">时间阈值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>旧文件列表</returns>
    Task<IReadOnlyList<FileEntry>> FindOldFilesAsync(
        string path,
        TimeSpan olderThan,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 重复文件组。
/// </summary>
public record DuplicateFileGroup
{
    /// <summary>
    /// 文件哈希值。
    /// </summary>
    public required string Hash { get; init; }

    /// <summary>
    /// 文件大小。
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// 重复文件列表。
    /// </summary>
    public required IReadOnlyList<FileEntry> Files { get; init; }

    /// <summary>
    /// 重复文件数量。
    /// </summary>
    public int Count => Files.Count;

    /// <summary>
    /// 可节省的空间（保留一个文件后）。
    /// </summary>
    public long WastedSpace => Size * (Count - 1);
}
