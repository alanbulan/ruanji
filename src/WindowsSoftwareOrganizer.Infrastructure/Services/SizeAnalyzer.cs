using System.Security.Cryptography;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// 大小分析器实现。
/// </summary>
public class SizeAnalyzer : ISizeAnalyzer
{
    private readonly IFileSystemService _fileSystemService;

    public SizeAnalyzer(IFileSystemService fileSystemService)
    {
        _fileSystemService = fileSystemService;
    }

    public async Task<SizeAnalysisResult> AnalyzeAsync(
        string path,
        int depth = 1,
        IProgress<SizeAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var items = new List<SizeAnalysisItem>();
        long totalSize = 0;
        int totalFiles = 0;
        int totalDirectories = 0;

        var dirInfo = new DirectoryInfo(path);
        if (!dirInfo.Exists)
        {
            return new SizeAnalysisResult
            {
                RootPath = path,
                TotalSize = 0,
                Items = items,
                TotalFiles = 0,
                TotalDirectories = 0,
                AnalysisDuration = TimeSpan.Zero
            };
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 分析子目录
        foreach (var subDir in dirInfo.EnumerateDirectories())
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            progress?.Report(new SizeAnalysisProgress
            {
                CurrentPath = subDir.FullName,
                DirectoriesScanned = totalDirectories
            });

            try
            {
                var (size, files, dirs) = await CalculateDirectorySizeAsync(subDir.FullName, depth - 1, cancellationToken);
                var percentage = totalSize > 0 ? (double)size / totalSize * 100 : 0;
                items.Add(new SizeAnalysisItem
                {
                    Name = subDir.Name,
                    FullPath = subDir.FullName,
                    Size = size,
                    IsDirectory = true,
                    Percentage = percentage
                });
                totalSize += size;
                totalFiles += files;
                totalDirectories += dirs + 1;
            }
            catch (UnauthorizedAccessException)
            {
                items.Add(new SizeAnalysisItem
                {
                    Name = subDir.Name,
                    FullPath = subDir.FullName,
                    Size = 0,
                    IsDirectory = true,
                    Percentage = 0
                });
                totalDirectories++;
            }
        }

        // 分析当前目录的文件
        foreach (var file in dirInfo.EnumerateFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            try
            {
                var fileSize = file.Length;
                var percentage = totalSize > 0 ? (double)fileSize / totalSize * 100 : 0;
                items.Add(new SizeAnalysisItem
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    Size = fileSize,
                    IsDirectory = false,
                    Percentage = percentage
                });
                totalSize += fileSize;
                totalFiles++;
            }
            catch { }
        }

        // 重新计算百分比
        var finalItems = items.Select(item => item with
        {
            Percentage = totalSize > 0 ? (double)item.Size / totalSize * 100 : 0
        }).OrderByDescending(i => i.Size).ToList();

        stopwatch.Stop();

        return new SizeAnalysisResult
        {
            RootPath = path,
            TotalSize = totalSize,
            Items = finalItems,
            TotalFiles = totalFiles,
            TotalDirectories = totalDirectories,
            AnalysisDuration = stopwatch.Elapsed
        };
    }

    public async Task<long> GetTotalSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            long size = 0;
            try
            {
                var dirInfo = new DirectoryInfo(path);
                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try { size += file.Length; } catch { }
                }
            }
            catch { }
            return size;
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<FileEntry>> FindLargestFilesAsync(
        string path,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var files = new List<FileEntry>();
            try
            {
                var dirInfo = new DirectoryInfo(path);
                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        files.Add(new FileEntry
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
                        });
                    }
                    catch { }
                }
            }
            catch { }

            return files.OrderByDescending(f => f.Size).Take(count).ToList();
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<SizeAnalysisItem>> FindLargestDirectoriesAsync(
        string path,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(async () =>
        {
            var items = new List<SizeAnalysisItem>();
            try
            {
                var dirInfo = new DirectoryInfo(path);
                foreach (var subDir in dirInfo.EnumerateDirectories())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var (size, files, dirs) = await CalculateDirectorySizeAsync(subDir.FullName, -1, cancellationToken);
                        items.Add(new SizeAnalysisItem
                        {
                            Name = subDir.Name,
                            FullPath = subDir.FullName,
                            Size = size,
                            IsDirectory = true,
                            Percentage = 0
                        });
                    }
                    catch { }
                }
            }
            catch { }

            return items.OrderByDescending(i => i.Size).Take(count).ToList();
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<DuplicateFileGroup>> FindDuplicateFilesAsync(
        string path,
        IProgress<SizeAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(async () =>
        {
            // 按大小分组
            var sizeGroups = new Dictionary<long, List<FileInfo>>();
            var dirInfo = new DirectoryInfo(path);
            
            progress?.Report(new SizeAnalysisProgress { CurrentPath = path });

            foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var size = file.Length;
                    if (size == 0) continue; // 跳过空文件
                    
                    if (!sizeGroups.ContainsKey(size))
                        sizeGroups[size] = new List<FileInfo>();
                    sizeGroups[size].Add(file);
                }
                catch { }
            }

            // 对相同大小的文件计算哈希
            var duplicates = new List<DuplicateFileGroup>();
            var potentialDuplicates = sizeGroups.Where(g => g.Value.Count > 1).ToList();
            var processed = 0;

            foreach (var group in potentialDuplicates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                progress?.Report(new SizeAnalysisProgress
                {
                    CurrentPath = "正在计算文件哈希",
                    FilesScanned = processed++
                });

                var hashGroups = new Dictionary<string, List<FileInfo>>();
                foreach (var file in group.Value)
                {
                    try
                    {
                        var hash = await ComputeFileHashAsync(file.FullName, cancellationToken);
                        if (!hashGroups.ContainsKey(hash))
                            hashGroups[hash] = new List<FileInfo>();
                        hashGroups[hash].Add(file);
                    }
                    catch { }
                }

                foreach (var hashGroup in hashGroups.Where(g => g.Value.Count > 1))
                {
                    duplicates.Add(new DuplicateFileGroup
                    {
                        Hash = hashGroup.Key,
                        Size = group.Key,
                        Files = hashGroup.Value.Select(f => new FileEntry
                        {
                            Name = f.Name,
                            FullPath = f.FullName,
                            Extension = f.Extension,
                            Size = f.Length,
                            CreatedTime = f.CreationTime,
                            ModifiedTime = f.LastWriteTime,
                            AccessedTime = f.LastAccessTime,
                            Attributes = f.Attributes,
                            Category = FileTypeCategoryHelper.GetCategory(f.Extension),
                            ParentPath = f.DirectoryName
                        }).ToList()
                    });
                }
            }

            return duplicates.OrderByDescending(d => d.WastedSpace).ToList();
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<DirectoryEntry>> FindEmptyDirectoriesAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var emptyDirs = new List<DirectoryEntry>();
            try
            {
                var dirInfo = new DirectoryInfo(path);
                foreach (var subDir in dirInfo.EnumerateDirectories("*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        if (!subDir.EnumerateFileSystemInfos().Any())
                        {
                            emptyDirs.Add(new DirectoryEntry
                            {
                                Name = subDir.Name,
                                FullPath = subDir.FullName,
                                CreatedTime = subDir.CreationTime,
                                ModifiedTime = subDir.LastWriteTime,
                                AccessedTime = subDir.LastAccessTime,
                                Attributes = subDir.Attributes,
                                HasSubdirectories = false,
                                IsAccessible = true,
                                ParentPath = subDir.Parent?.FullName
                            });
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return emptyDirs;
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<FileEntry>> FindOldFilesAsync(
        string path,
        TimeSpan olderThan,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var oldFiles = new List<FileEntry>();
            var threshold = DateTime.Now - olderThan;
            
            try
            {
                var dirInfo = new DirectoryInfo(path);
                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        if (file.LastAccessTime < threshold)
                        {
                            oldFiles.Add(new FileEntry
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
                            });
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return oldFiles.OrderBy(f => f.AccessedTime).ToList();
        }, cancellationToken);
    }

    private async Task<(long Size, int Files, int Dirs)> CalculateDirectorySizeAsync(
        string path, int depth, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            long size = 0;
            int files = 0;
            int dirs = 0;

            try
            {
                var dirInfo = new DirectoryInfo(path);
                
                foreach (var file in dirInfo.EnumerateFiles())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try { size += file.Length; files++; } catch { }
                }

                if (depth != 0)
                {
                    foreach (var subDir in dirInfo.EnumerateDirectories())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            var (subSize, subFiles, subDirs) = CalculateDirectorySizeAsync(
                                subDir.FullName, depth - 1, cancellationToken).Result;
                            size += subSize;
                            files += subFiles;
                            dirs += subDirs + 1;
                        }
                        catch { dirs++; }
                    }
                }
            }
            catch { }

            return (size, files, dirs);
        }, cancellationToken);
    }

    private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using var md5 = MD5.Create();
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        var hash = await md5.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}
