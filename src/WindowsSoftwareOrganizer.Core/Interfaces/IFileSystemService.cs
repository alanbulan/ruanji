using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Core.Interfaces;

/// <summary>
/// 文件系统服务接口 - 提供文件和目录的基本操作。
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// 获取所有可用驱动器。
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>驱动器列表</returns>
    Task<IReadOnlyList<DriveEntry>> GetDrivesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定路径下的目录列表。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>目录列表</returns>
    Task<IReadOnlyList<DirectoryEntry>> GetDirectoriesAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定路径下的文件列表。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件列表</returns>
    Task<IReadOnlyList<FileEntry>> GetFilesAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取目录的完整内容（目录和文件）。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="filter">筛选选项</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>目录内容</returns>
    Task<DirectoryContent> GetDirectoryContentAsync(
        string path, 
        FileFilterOptions? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查路径是否存在。
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>是否存在</returns>
    bool Exists(string path);

    /// <summary>
    /// 检查路径是否为目录。
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>是否为目录</returns>
    bool IsDirectory(string path);

    /// <summary>
    /// 检查路径是否可访问。
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>是否可访问</returns>
    bool IsAccessible(string path);

    /// <summary>
    /// 使用默认程序打开文件。
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>操作结果</returns>
    Task<FileOperationResult> OpenFileAsync(string filePath);

    /// <summary>
    /// 在资源管理器中打开并选中文件/目录。
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>操作结果</returns>
    Task<FileOperationResult> OpenInExplorerAsync(string path);

    /// <summary>
    /// 获取特殊文件夹列表。
    /// </summary>
    /// <returns>特殊文件夹信息列表</returns>
    IReadOnlyList<SpecialFolderInfo> GetSpecialFolders();

    /// <summary>
    /// 获取特殊文件夹路径。
    /// </summary>
    /// <param name="folderType">特殊文件夹类型</param>
    /// <returns>文件夹路径，如果不存在则返回 null</returns>
    string? GetSpecialFolderPath(SpecialFolderType folderType);

    /// <summary>
    /// 创建目录。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <returns>操作结果</returns>
    Task<FileOperationResult> CreateDirectoryAsync(string path);

    /// <summary>
    /// 删除文件或目录。
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="useRecycleBin">是否移动到回收站</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<FileOperationResult> DeleteAsync(string path, bool useRecycleBin = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// 复制文件或目录。
    /// </summary>
    /// <param name="sourcePath">源路径</param>
    /// <param name="destinationPath">目标路径</param>
    /// <param name="overwrite">是否覆盖</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<FileOperationResult> CopyAsync(
        string sourcePath, 
        string destinationPath, 
        bool overwrite = false,
        IProgress<FileOperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 移动文件或目录。
    /// </summary>
    /// <param name="sourcePath">源路径</param>
    /// <param name="destinationPath">目标路径</param>
    /// <param name="overwrite">是否覆盖</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<FileOperationResult> MoveAsync(
        string sourcePath, 
        string destinationPath, 
        bool overwrite = false,
        IProgress<FileOperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 重命名文件或目录。
    /// </summary>
    /// <param name="path">原路径</param>
    /// <param name="newName">新名称</param>
    /// <returns>操作结果</returns>
    Task<FileOperationResult> RenameAsync(string path, string newName);

    /// <summary>
    /// 获取文件或目录的大小。
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>大小（字节）</returns>
    Task<long> GetSizeAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取目录中的文件数量。
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="recursive">是否递归</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件数量</returns>
    Task<int> GetFileCountAsync(string path, bool recursive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取父目录路径。
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>父目录路径，如果没有则返回 null</returns>
    string? GetParentPath(string path);

    /// <summary>
    /// 规范化路径。
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>规范化后的路径</returns>
    string NormalizePath(string path);

    /// <summary>
    /// 组合路径。
    /// </summary>
    /// <param name="paths">路径部分</param>
    /// <returns>组合后的路径</returns>
    string CombinePath(params string[] paths);
}

/// <summary>
/// 文件操作进度。
/// </summary>
public record FileOperationProgress
{
    /// <summary>
    /// 当前处理的文件/目录名。
    /// </summary>
    public string CurrentItem { get; init; } = string.Empty;

    /// <summary>
    /// 已处理的项目数。
    /// </summary>
    public int ProcessedItems { get; init; }

    /// <summary>
    /// 总项目数。
    /// </summary>
    public int TotalItems { get; init; }

    /// <summary>
    /// 已处理的字节数。
    /// </summary>
    public long ProcessedBytes { get; init; }

    /// <summary>
    /// 总字节数。
    /// </summary>
    public long TotalBytes { get; init; }

    /// <summary>
    /// 进度百分比（0-100）。
    /// </summary>
    public double Percentage => TotalBytes > 0 ? (double)ProcessedBytes / TotalBytes * 100 : 
                                TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;

    /// <summary>
    /// 操作类型。
    /// </summary>
    public FileOperationType OperationType { get; init; }
}
