using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// AI 文件分析器接口 - 使用 AI 分析文件并提供整理建议。
/// </summary>
public interface IAIFileAnalyzer
{
    /// <summary>
    /// 分析指定目录并生成整理建议。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="options">分析选项</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AI 分析结果</returns>
    Task<AIAnalysisResult> AnalyzeAsync(
        string path,
        AIAnalysisOptions? options = null,
        IProgress<AIAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 分析指定文件列表并生成整理建议。
    /// </summary>
    /// <param name="files">文件列表</param>
    /// <param name="options">分析选项</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AI 分析结果</returns>
    Task<AIAnalysisResult> AnalyzeFilesAsync(
        IReadOnlyList<FileEntry> files,
        AIAnalysisOptions? options = null,
        IProgress<AIAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 为文件生成建议的新名称。
    /// </summary>
    /// <param name="file">文件信息</param>
    /// <param name="context">上下文信息（如同目录下的其他文件）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>建议的新名称列表</returns>
    Task<IReadOnlyList<string>> SuggestFileNameAsync(
        FileEntry file,
        string? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 为文件建议分类目录。
    /// </summary>
    /// <param name="file">文件信息</param>
    /// <param name="availableDirectories">可用的目标目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>建议的目标目录</returns>
    Task<string?> SuggestCategoryDirectoryAsync(
        FileEntry file,
        IReadOnlyList<string> availableDirectories,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查 AI 服务是否可用。
    /// </summary>
    /// <returns>是否可用</returns>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// 获取当前配置状态。
    /// </summary>
    /// <returns>配置是否有效</returns>
    bool IsConfigured { get; }
}
