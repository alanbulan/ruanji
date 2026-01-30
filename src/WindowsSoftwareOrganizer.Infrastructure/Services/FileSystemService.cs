using System.Diagnostics;
using System.Runtime.InteropServices;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Infrastructure.Services;

/// <summary>
/// 文件系统服务实现。
/// </summary>
public class FileSystemService : IFileSystemService
{
    // Shell32 API for recycle bin
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        public string pFrom;
        public string pTo;
        public ushort fFlags;
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string lpszProgressTitle;
    }

    private const uint FO_DELETE = 0x0003;
    private const ushort FOF_ALLOWUNDO = 0x0040;
    private const ushort FOF_NOCONFIRMATION = 0x0010;
    private const ushort FOF_SILENT = 0x0004;

    public async Task<IReadOnlyList<DriveEntry>> GetDrivesAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var drives = new List<DriveEntry>();
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    drives.Add(new DriveEntry
                    {
                        Name = drive.Name,
                        RootPath = drive.RootDirectory.FullName,
                        DriveType = drive.DriveType,
                        VolumeLabel = drive.IsReady ? drive.VolumeLabel : string.Empty,
                        TotalSize = drive.IsReady ? drive.TotalSize : 0,
                        FreeSpace = drive.IsReady ? drive.AvailableFreeSpace : 0,
                        IsReady = drive.IsReady,
                        FileSystem = drive.IsReady ? drive.DriveFormat : string.Empty
                    });
                }
                catch
                {
                    // 忽略无法访问的驱动器
                    drives.Add(new DriveEntry
                    {
                        Name = drive.Name,
                        RootPath = drive.RootDirectory.FullName,
                        DriveType = drive.DriveType,
                        VolumeLabel = string.Empty,
                        IsReady = false
                    });
                }
            }
            return drives;
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<DirectoryEntry>> GetDirectoriesAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var directories = new List<DirectoryEntry>();
            try
            {
                var dirInfo = new DirectoryInfo(path);
                foreach (var dir in dirInfo.EnumerateDirectories())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        directories.Add(CreateDirectoryEntry(dir));
                    }
                    catch
                    {
                        // 添加不可访问的目录
                        directories.Add(new DirectoryEntry
                        {
                            Name = dir.Name,
                            FullPath = dir.FullName,
                            IsAccessible = false,
                            ParentPath = path
                        });
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            return directories;
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<FileEntry>> GetFilesAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var files = new List<FileEntry>();
            try
            {
                var dirInfo = new DirectoryInfo(path);
                foreach (var file in dirInfo.EnumerateFiles())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        files.Add(CreateFileEntry(file));
                    }
                    catch { }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            return files;
        }, cancellationToken);
    }

    public async Task<DirectoryContent> GetDirectoryContentAsync(
        string path,
        FileFilterOptions? filter = null,
        CancellationToken cancellationToken = default)
    {
        var directories = await GetDirectoriesAsync(path, cancellationToken);
        var files = await GetFilesAsync(path, cancellationToken);

        // 应用筛选
        if (filter != null)
        {
            directories = ApplyDirectoryFilter(directories, filter);
            files = ApplyFileFilter(files, filter);
        }

        return new DirectoryContent
        {
            Path = path,
            Directories = directories,
            Files = files,
            ParentPath = GetParentPath(path)
        };
    }

    public bool Exists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    public bool IsDirectory(string path)
    {
        return Directory.Exists(path);
    }

    public bool IsAccessible(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.GetDirectories(path);
                return true;
            }
            if (File.Exists(path))
            {
                using var _ = File.OpenRead(path);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<FileOperationResult> OpenFileAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        OperationType = FileOperationType.CreateFile,
                        SourcePath = filePath,
                        ErrorMessage = "文件不存在"
                    };
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

                return new FileOperationResult
                {
                    Success = true,
                    OperationType = FileOperationType.CreateFile,
                    SourcePath = filePath
                };
            }
            catch (Exception ex)
            {
                return new FileOperationResult
                {
                    Success = false,
                    OperationType = FileOperationType.CreateFile,
                    SourcePath = filePath,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
        });
    }

    public async Task<FileOperationResult> OpenInExplorerAsync(string path)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (File.Exists(path))
                {
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
                else if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", $"\"{path}\"");
                }
                else
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        OperationType = FileOperationType.CreateFile,
                        SourcePath = path,
                        ErrorMessage = "路径不存在"
                    };
                }

                return new FileOperationResult
                {
                    Success = true,
                    OperationType = FileOperationType.CreateFile,
                    SourcePath = path
                };
            }
            catch (Exception ex)
            {
                return new FileOperationResult
                {
                    Success = false,
                    OperationType = FileOperationType.CreateFile,
                    SourcePath = path,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
        });
    }

    public IReadOnlyList<SpecialFolderInfo> GetSpecialFolders()
    {
        var folders = new List<SpecialFolderInfo>();
        var folderTypes = Enum.GetValues<SpecialFolderType>();

        foreach (var type in folderTypes)
        {
            var path = GetSpecialFolderPath(type);
            if (!string.IsNullOrEmpty(path))
            {
                folders.Add(new SpecialFolderInfo
                {
                    Type = type,
                    Path = path,
                    DisplayName = GetSpecialFolderDisplayName(type),
                    IconGlyph = GetSpecialFolderIcon(type),
                    Exists = Directory.Exists(path)
                });
            }
        }

        return folders;
    }

    public string? GetSpecialFolderPath(SpecialFolderType folderType)
    {
        try
        {
            return folderType switch
            {
                SpecialFolderType.Desktop => Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                SpecialFolderType.Documents => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                SpecialFolderType.Downloads => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                SpecialFolderType.Music => Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                SpecialFolderType.Pictures => Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                SpecialFolderType.Videos => Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                SpecialFolderType.UserProfile => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                SpecialFolderType.ProgramFiles => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                SpecialFolderType.ProgramFilesX86 => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                SpecialFolderType.AppData => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                SpecialFolderType.LocalAppData => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                SpecialFolderType.CommonAppData => Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                SpecialFolderType.Temp => Path.GetTempPath(),
                SpecialFolderType.Recent => Environment.GetFolderPath(Environment.SpecialFolder.Recent),
                SpecialFolderType.Favorites => Environment.GetFolderPath(Environment.SpecialFolder.Favorites),
                SpecialFolderType.StartMenu => Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                SpecialFolderType.Startup => Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                SpecialFolderType.SendTo => Environment.GetFolderPath(Environment.SpecialFolder.SendTo),
                SpecialFolderType.Fonts => Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
                SpecialFolderType.CommonDocuments => Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                SpecialFolderType.CommonMusic => Environment.GetFolderPath(Environment.SpecialFolder.CommonMusic),
                SpecialFolderType.CommonPictures => Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures),
                SpecialFolderType.CommonVideos => Environment.GetFolderPath(Environment.SpecialFolder.CommonVideos),
                SpecialFolderType.OneDrive => GetOneDrivePath(),
                SpecialFolderType.NetworkShortcuts => Environment.GetFolderPath(Environment.SpecialFolder.NetworkShortcuts),
                SpecialFolderType.PrinterShortcuts => Environment.GetFolderPath(Environment.SpecialFolder.PrinterShortcuts),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<FileOperationResult> CreateDirectoryAsync(string path)
    {
        return await Task.Run(() =>
        {
            try
            {
                Directory.CreateDirectory(path);
                return new FileOperationResult
                {
                    Success = true,
                    OperationType = FileOperationType.CreateDirectory,
                    SourcePath = path
                };
            }
            catch (Exception ex)
            {
                return new FileOperationResult
                {
                    Success = false,
                    OperationType = FileOperationType.CreateDirectory,
                    SourcePath = path,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
        });
    }

    public async Task<FileOperationResult> DeleteAsync(string path, bool useRecycleBin = true, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (useRecycleBin)
                {
                    return DeleteToRecycleBin(path);
                }

                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                else
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        OperationType = FileOperationType.Delete,
                        SourcePath = path,
                        ErrorMessage = "路径不存在"
                    };
                }

                return new FileOperationResult
                {
                    Success = true,
                    OperationType = FileOperationType.Delete,
                    SourcePath = path
                };
            }
            catch (Exception ex)
            {
                return new FileOperationResult
                {
                    Success = false,
                    OperationType = FileOperationType.Delete,
                    SourcePath = path,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
        }, cancellationToken);
    }

    public async Task<FileOperationResult> CopyAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite = false,
        IProgress<FileOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (File.Exists(sourcePath))
                {
                    var destDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    File.Copy(sourcePath, destinationPath, overwrite);
                }
                else if (Directory.Exists(sourcePath))
                {
                    CopyDirectory(sourcePath, destinationPath, overwrite, progress, cancellationToken);
                }
                else
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        OperationType = FileOperationType.Copy,
                        SourcePath = sourcePath,
                        DestinationPath = destinationPath,
                        ErrorMessage = "源路径不存在"
                    };
                }

                return new FileOperationResult
                {
                    Success = true,
                    OperationType = FileOperationType.Copy,
                    SourcePath = sourcePath,
                    DestinationPath = destinationPath
                };
            }
            catch (Exception ex)
            {
                return new FileOperationResult
                {
                    Success = false,
                    OperationType = FileOperationType.Copy,
                    SourcePath = sourcePath,
                    DestinationPath = destinationPath,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
        }, cancellationToken);
    }

    public async Task<FileOperationResult> MoveAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite = false,
        IProgress<FileOperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (File.Exists(sourcePath))
                {
                    var destDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    if (overwrite && File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath);
                    }
                    File.Move(sourcePath, destinationPath);
                }
                else if (Directory.Exists(sourcePath))
                {
                    if (overwrite && Directory.Exists(destinationPath))
                    {
                        Directory.Delete(destinationPath, true);
                    }
                    Directory.Move(sourcePath, destinationPath);
                }
                else
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        OperationType = FileOperationType.Move,
                        SourcePath = sourcePath,
                        DestinationPath = destinationPath,
                        ErrorMessage = "源路径不存在"
                    };
                }

                return new FileOperationResult
                {
                    Success = true,
                    OperationType = FileOperationType.Move,
                    SourcePath = sourcePath,
                    DestinationPath = destinationPath
                };
            }
            catch (Exception ex)
            {
                return new FileOperationResult
                {
                    Success = false,
                    OperationType = FileOperationType.Move,
                    SourcePath = sourcePath,
                    DestinationPath = destinationPath,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
        }, cancellationToken);
    }

    public async Task<FileOperationResult> RenameAsync(string path, string newName)
    {
        return await Task.Run(() =>
        {
            try
            {
                var parentDir = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(parentDir))
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        OperationType = FileOperationType.Rename,
                        SourcePath = path,
                        ErrorMessage = "无法获取父目录"
                    };
                }

                var newPath = Path.Combine(parentDir, newName);

                if (File.Exists(path))
                {
                    File.Move(path, newPath);
                }
                else if (Directory.Exists(path))
                {
                    Directory.Move(path, newPath);
                }
                else
                {
                    return new FileOperationResult
                    {
                        Success = false,
                        OperationType = FileOperationType.Rename,
                        SourcePath = path,
                        ErrorMessage = "路径不存在"
                    };
                }

                return new FileOperationResult
                {
                    Success = true,
                    OperationType = FileOperationType.Rename,
                    SourcePath = path,
                    DestinationPath = newPath
                };
            }
            catch (Exception ex)
            {
                return new FileOperationResult
                {
                    Success = false,
                    OperationType = FileOperationType.Rename,
                    SourcePath = path,
                    ErrorMessage = ex.Message,
                    Exception = ex
                };
            }
        });
    }

    public async Task<long> GetSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (File.Exists(path))
            {
                return new FileInfo(path).Length;
            }
            if (Directory.Exists(path))
            {
                return GetDirectorySize(path, cancellationToken);
            }
            return 0L;
        }, cancellationToken);
    }

    public async Task<int> GetFileCountAsync(string path, bool recursive = false, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                return Directory.GetFiles(path, "*", searchOption).Length;
            }
            catch
            {
                return 0;
            }
        }, cancellationToken);
    }

    public string? GetParentPath(string path)
    {
        try
        {
            var parent = Directory.GetParent(path);
            return parent?.FullName;
        }
        catch
        {
            return null;
        }
    }

    public string NormalizePath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public string CombinePath(params string[] paths)
    {
        return Path.Combine(paths);
    }

    #region Private Methods

    private DirectoryEntry CreateDirectoryEntry(DirectoryInfo dir)
    {
        bool hasSubdirs;
        try
        {
            hasSubdirs = dir.EnumerateDirectories().Any();
        }
        catch
        {
            hasSubdirs = false;
        }

        return new DirectoryEntry
        {
            Name = dir.Name,
            FullPath = dir.FullName,
            CreatedTime = dir.CreationTime,
            ModifiedTime = dir.LastWriteTime,
            AccessedTime = dir.LastAccessTime,
            Attributes = dir.Attributes,
            HasSubdirectories = hasSubdirs,
            IsAccessible = true,
            ParentPath = dir.Parent?.FullName
        };
    }

    private FileEntry CreateFileEntry(FileInfo file)
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

    private IReadOnlyList<DirectoryEntry> ApplyDirectoryFilter(IReadOnlyList<DirectoryEntry> directories, FileFilterOptions filter)
    {
        var result = directories.AsEnumerable();

        if (!filter.ShowHiddenFiles)
        {
            result = result.Where(d => !d.IsHidden);
        }
        if (!filter.ShowSystemFiles)
        {
            result = result.Where(d => !d.IsSystem);
        }
        if (!string.IsNullOrEmpty(filter.NamePattern))
        {
            result = result.Where(d => MatchesPattern(d.Name, filter.NamePattern));
        }

        return result.ToList();
    }

    private IReadOnlyList<FileEntry> ApplyFileFilter(IReadOnlyList<FileEntry> files, FileFilterOptions filter)
    {
        var result = files.AsEnumerable();

        if (!filter.ShowHiddenFiles)
        {
            result = result.Where(f => !f.IsHidden);
        }
        if (!filter.ShowSystemFiles)
        {
            result = result.Where(f => !f.IsSystem);
        }
        if (filter.IncludeCategories?.Count > 0)
        {
            result = result.Where(f => filter.IncludeCategories.Contains(f.Category));
        }
        if (filter.ExcludeCategories?.Count > 0)
        {
            result = result.Where(f => !filter.ExcludeCategories.Contains(f.Category));
        }
        if (filter.IncludeExtensions?.Count > 0)
        {
            result = result.Where(f => filter.IncludeExtensions.Any(e => 
                f.Extension.Equals(e, StringComparison.OrdinalIgnoreCase)));
        }
        if (filter.ExcludeExtensions?.Count > 0)
        {
            result = result.Where(f => !filter.ExcludeExtensions.Any(e => 
                f.Extension.Equals(e, StringComparison.OrdinalIgnoreCase)));
        }
        if (filter.MinSize.HasValue)
        {
            result = result.Where(f => f.Size >= filter.MinSize.Value);
        }
        if (filter.MaxSize.HasValue)
        {
            result = result.Where(f => f.Size <= filter.MaxSize.Value);
        }
        if (filter.ModifiedAfter.HasValue)
        {
            result = result.Where(f => f.ModifiedTime >= filter.ModifiedAfter.Value);
        }
        if (filter.ModifiedBefore.HasValue)
        {
            result = result.Where(f => f.ModifiedTime <= filter.ModifiedBefore.Value);
        }
        if (!string.IsNullOrEmpty(filter.NamePattern))
        {
            result = result.Where(f => MatchesPattern(f.Name, filter.NamePattern));
        }

        return result.ToList();
    }

    private bool MatchesPattern(string name, string pattern)
    {
        // 简单通配符匹配
        var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return System.Text.RegularExpressions.Regex.IsMatch(name, regex, 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private FileOperationResult DeleteToRecycleBin(string path)
    {
        var fileOp = new SHFILEOPSTRUCT
        {
            wFunc = FO_DELETE,
            pFrom = path + '\0' + '\0',
            fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT
        };

        int result = SHFileOperation(ref fileOp);
        
        return new FileOperationResult
        {
            Success = result == 0,
            OperationType = FileOperationType.Delete,
            SourcePath = path,
            ErrorMessage = result != 0 ? $"删除失败，错误代码: {result}" : null
        };
    }

    private void CopyDirectory(string sourceDir, string destDir, bool overwrite, 
        IProgress<FileOperationProgress>? progress, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destDir);

        var files = Directory.GetFiles(sourceDir);
        var dirs = Directory.GetDirectories(sourceDir);
        var totalItems = files.Length + dirs.Length;
        var processedItems = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite);
            processedItems++;
            progress?.Report(new FileOperationProgress
            {
                CurrentItem = file,
                ProcessedItems = processedItems,
                TotalItems = totalItems,
                OperationType = FileOperationType.Copy
            });
        }

        foreach (var dir in dirs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir, overwrite, progress, cancellationToken);
            processedItems++;
        }
    }

    private long GetDirectorySize(string path, CancellationToken cancellationToken)
    {
        long size = 0;
        try
        {
            var dirInfo = new DirectoryInfo(path);
            foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    size += file.Length;
                }
                catch { }
            }
        }
        catch { }
        return size;
    }

    private string? GetOneDrivePath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var oneDrivePath = Path.Combine(userProfile, "OneDrive");
        return Directory.Exists(oneDrivePath) ? oneDrivePath : null;
    }

    private string GetSpecialFolderDisplayName(SpecialFolderType type)
    {
        return type switch
        {
            SpecialFolderType.Desktop => "桌面",
            SpecialFolderType.Documents => "文档",
            SpecialFolderType.Downloads => "下载",
            SpecialFolderType.Music => "音乐",
            SpecialFolderType.Pictures => "图片",
            SpecialFolderType.Videos => "视频",
            SpecialFolderType.UserProfile => "用户目录",
            SpecialFolderType.ProgramFiles => "程序文件",
            SpecialFolderType.ProgramFilesX86 => "程序文件 (x86)",
            SpecialFolderType.AppData => "应用数据",
            SpecialFolderType.LocalAppData => "本地应用数据",
            SpecialFolderType.CommonAppData => "公共应用数据",
            SpecialFolderType.Temp => "临时文件",
            SpecialFolderType.Recent => "最近使用",
            SpecialFolderType.Favorites => "收藏夹",
            SpecialFolderType.StartMenu => "开始菜单",
            SpecialFolderType.Startup => "启动",
            SpecialFolderType.SendTo => "发送到",
            SpecialFolderType.Fonts => "字体",
            SpecialFolderType.OneDrive => "OneDrive",
            _ => type.ToString()
        };
    }

    private string GetSpecialFolderIcon(SpecialFolderType type)
    {
        return type switch
        {
            SpecialFolderType.Desktop => "\uE8FC",
            SpecialFolderType.Documents => "\uE8A5",
            SpecialFolderType.Downloads => "\uE896",
            SpecialFolderType.Music => "\uE8D6",
            SpecialFolderType.Pictures => "\uEB9F",
            SpecialFolderType.Videos => "\uE8B2",
            SpecialFolderType.OneDrive => "\uE753",
            _ => "\uE8B7"
        };
    }

    #endregion
}
