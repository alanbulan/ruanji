using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// 文件搜索服务接口 - 提供文件搜索功能。
/// </summary>
public interface IFileSearchService
{
    /// <summary>
    /// 搜索文件。
    /// </summary>
    /// <param name="criteria">搜索条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索结果（异步流）</returns>
    IAsyncEnumerable<FileEntry> SearchAsync(
        FileSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索文件并返回完整结果。
    /// </summary>
    /// <param name="criteria">搜索条件</param>
    /// <param name="maxResults">最大结果数</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>搜索结果列表</returns>
    Task<FileSearchResult> SearchAllAsync(
        FileSearchCriteria criteria,
        int maxResults = 1000,
        IProgress<FileSearchProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 快速搜索文件名。
    /// </summary>
    /// <param name="path">搜索路径</param>
    /// <param name="pattern">文件名模式（支持通配符）</param>
    /// <param name="recursive">是否递归</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配的文件列表</returns>
    IAsyncEnumerable<FileEntry> QuickSearchAsync(
        string path,
        string pattern,
        bool recursive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索目录。
    /// </summary>
    /// <param name="path">搜索路径</param>
    /// <param name="pattern">目录名模式</param>
    /// <param name="recursive">是否递归</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>匹配的目录列表</returns>
    IAsyncEnumerable<DirectoryEntry> SearchDirectoriesAsync(
        string path,
        string pattern,
        bool recursive = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索文件内容。
    /// </summary>
    /// <param name="path">搜索路径</param>
    /// <param name="content">要搜索的内容</param>
    /// <param name="filePattern">文件名模式</param>
    /// <param name="caseSensitive">是否区分大小写</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>包含指定内容的文件列表</returns>
    IAsyncEnumerable<FileContentMatch> SearchContentAsync(
        string path,
        string content,
        string? filePattern = null,
        bool caseSensitive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近修改的文件。
    /// </summary>
    /// <param name="path">搜索路径</param>
    /// <param name="count">返回数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最近修改的文件列表</returns>
    Task<IReadOnlyList<FileEntry>> GetRecentFilesAsync(
        string path,
        int count = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最近访问的文件。
    /// </summary>
    /// <param name="path">搜索路径</param>
    /// <param name="count">返回数量</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最近访问的文件列表</returns>
    Task<IReadOnlyList<FileEntry>> GetRecentlyAccessedFilesAsync(
        string path,
        int count = 20,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 文件搜索结果。
/// </summary>
public record FileSearchResult
{
    /// <summary>
    /// 搜索条件。
    /// </summary>
    public required FileSearchCriteria Criteria { get; init; }

    /// <summary>
    /// 匹配的文件列表。
    /// </summary>
    public required IReadOnlyList<FileEntry> Files { get; init; }

    /// <summary>
    /// 搜索的目录数。
    /// </summary>
    public int DirectoriesSearched { get; init; }

    /// <summary>
    /// 搜索的文件数。
    /// </summary>
    public int FilesSearched { get; init; }

    /// <summary>
    /// 搜索耗时。
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// 是否达到最大结果数限制。
    /// </summary>
    public bool ReachedLimit { get; init; }

    /// <summary>
    /// 匹配的文件数。
    /// </summary>
    public int MatchCount => Files.Count;

    /// <summary>
    /// 匹配文件的总大小。
    /// </summary>
    public long TotalSize => Files.Sum(f => f.Size);
}

/// <summary>
/// 文件搜索进度。
/// </summary>
public record FileSearchProgress
{
    /// <summary>
    /// 当前搜索的目录。
    /// </summary>
    public string CurrentDirectory { get; init; } = string.Empty;

    /// <summary>
    /// 已搜索的目录数。
    /// </summary>
    public int DirectoriesSearched { get; init; }

    /// <summary>
    /// 已搜索的文件数。
    /// </summary>
    public int FilesSearched { get; init; }

    /// <summary>
    /// 已找到的匹配数。
    /// </summary>
    public int MatchesFound { get; init; }
}

/// <summary>
/// 文件内容匹配结果。
/// </summary>
public record FileContentMatch
{
    /// <summary>
    /// 匹配的文件。
    /// </summary>
    public required FileEntry File { get; init; }

    /// <summary>
    /// 匹配的行列表。
    /// </summary>
    public required IReadOnlyList<ContentMatchLine> Matches { get; init; }

    /// <summary>
    /// 匹配次数。
    /// </summary>
    public int MatchCount => Matches.Count;
}

/// <summary>
/// 内容匹配行。
/// </summary>
public record ContentMatchLine
{
    /// <summary>
    /// 行号。
    /// </summary>
    public int LineNumber { get; init; }

    /// <summary>
    /// 行内容。
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// 匹配位置（列号）。
    /// </summary>
    public int MatchPosition { get; init; }

    /// <summary>
    /// 匹配长度。
    /// </summary>
    public int MatchLength { get; init; }
}
