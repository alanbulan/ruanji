using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// 文件类型统计服务实现。
/// </summary>
public class TypeStatisticsService : ITypeStatisticsService
{
    public async Task<TypeStatisticsResult> AnalyzeAsync(
        string path,
        bool recursive = true,
        IProgress<TypeStatisticsProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var categoryStats = new Dictionary<FileTypeCategory, (int Count, long Size)>();
        int totalFiles = 0;
        long totalSize = 0;
        int processedDirs = 0;

        await Task.Run(() =>
        {
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists) return;

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            
            foreach (var file in dirInfo.EnumerateFiles("*", searchOption))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var category = FileTypeCategoryHelper.GetCategory(file.Extension);
                    var size = file.Length;

                    if (!categoryStats.ContainsKey(category))
                        categoryStats[category] = (0, 0);

                    var current = categoryStats[category];
                    categoryStats[category] = (current.Count + 1, current.Size + size);

                    totalFiles++;
                    totalSize += size;

                    // 每处理 100 个文件报告一次进度
                    if (totalFiles % 100 == 0)
                    {
                        progress?.Report(new TypeStatisticsProgress
                        {
                            CurrentDirectory = file.DirectoryName ?? path,
                            ProcessedFiles = totalFiles,
                            ProcessedDirectories = processedDirs,
                            ProcessedSize = totalSize
                        });
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }, cancellationToken);

        // 构建结果
        var items = categoryStats
            .Select(kvp => new TypeStatisticsItem
            {
                Extension = kvp.Key.ToString(),
                Category = kvp.Key,
                FileCount = kvp.Value.Count,
                TotalSize = kvp.Value.Size,
                SizePercentage = totalSize > 0 ? (double)kvp.Value.Size / totalSize * 100 : 0,
                CountPercentage = totalFiles > 0 ? (double)kvp.Value.Count / totalFiles * 100 : 0
            })
            .OrderByDescending(i => i.TotalSize)
            .ToList();

        return new TypeStatisticsResult
        {
            RootPath = path,
            Items = items,
            TotalFiles = totalFiles,
            TotalSize = totalSize
        };
    }

    public async Task<IReadOnlyList<ExtensionStatistics>> GetExtensionStatisticsAsync(
        string path,
        bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        var extensionStats = new Dictionary<string, ExtensionStatsBuilder>(StringComparer.OrdinalIgnoreCase);

        await Task.Run(() =>
        {
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists) return;

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var file in dirInfo.EnumerateFiles("*", searchOption))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var ext = string.IsNullOrEmpty(file.Extension) ? "(无扩展名)" : file.Extension.ToLowerInvariant();
                    var size = file.Length;

                    if (!extensionStats.TryGetValue(ext, out var builder))
                    {
                        builder = new ExtensionStatsBuilder
                        {
                            Extension = ext,
                            Category = FileTypeCategoryHelper.GetCategory(file.Extension),
                            MinSize = long.MaxValue
                        };
                        extensionStats[ext] = builder;
                    }

                    builder.Count++;
                    builder.TotalSize += size;
                    if (size > builder.MaxSize) builder.MaxSize = size;
                    if (size < builder.MinSize) builder.MinSize = size;
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }, cancellationToken);

        return extensionStats.Values
            .Select(b => new ExtensionStatistics
            {
                Extension = b.Extension,
                Category = b.Category,
                Count = b.Count,
                TotalSize = b.TotalSize,
                MaxSize = b.MaxSize,
                MinSize = b.MinSize == long.MaxValue ? 0 : b.MinSize
            })
            .OrderByDescending(e => e.TotalSize)
            .ToList();
    }

    public async Task<IReadOnlyList<FileEntry>> GetFilesByCategoryAsync(
        string path,
        FileTypeCategory category,
        bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        var files = new List<FileEntry>();

        await Task.Run(() =>
        {
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists) return;

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var file in dirInfo.EnumerateFiles("*", searchOption))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (FileTypeCategoryHelper.GetCategory(file.Extension) == category)
                    {
                        files.Add(CreateFileEntry(file));
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }, cancellationToken);

        return files.OrderByDescending(f => f.Size).ToList();
    }

    public async Task<IReadOnlyList<FileEntry>> GetFilesByExtensionAsync(
        string path,
        string extension,
        bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        var files = new List<FileEntry>();
        var normalizedExt = extension.StartsWith(".") ? extension : "." + extension;

        await Task.Run(() =>
        {
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists) return;

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            foreach (var file in dirInfo.EnumerateFiles("*" + normalizedExt, searchOption))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    files.Add(CreateFileEntry(file));
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
            }
        }, cancellationToken);

        return files.OrderByDescending(f => f.Size).ToList();
    }

    private static FileEntry CreateFileEntry(FileInfo file)
    {
        return new FileEntry
        {
            Name = file.Name,
            FullPath = file.FullName,
            Extension = file.Extension,
            Size = file.Length,
            CreatedTime = file.CreationTime,
            ModifiedTime = file.LastWriteTime,
            AccessedTime = file.LastAccessTime,
            Attributes = file.Attributes,
            Category = FileTypeCategoryHelper.GetCategory(file.Extension),
            ParentPath = file.DirectoryName
        };
    }

    private class ExtensionStatsBuilder
    {
        public string Extension { get; set; } = string.Empty;
        public FileTypeCategory Category { get; set; }
        public int Count { get; set; }
        public long TotalSize { get; set; }
        public long MaxSize { get; set; }
        public long MinSize { get; set; }
    }
}
