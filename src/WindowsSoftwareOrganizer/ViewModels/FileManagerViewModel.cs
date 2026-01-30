using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.ViewModels;

/// <summary>
/// 文件管理器 ViewModel。
/// </summary>
public partial class FileManagerViewModel : ObservableObject
{
    private readonly IFileSystemService _fileSystemService;
    private readonly ISizeAnalyzer _sizeAnalyzer;
    private readonly ITypeStatisticsService _typeStatisticsService;
    private readonly IAIFileAnalyzer _aiFileAnalyzer;
    private readonly IBatchFileOperator _batchFileOperator;
    private readonly IFileSearchService _fileSearchService;
    private DispatcherQueue? _dispatcherQueue;

    private DispatcherQueue DispatcherQueue => _dispatcherQueue ??= DispatcherQueue.GetForCurrentThread();

    #region 属性

    [ObservableProperty]
    private ObservableCollection<DriveEntry> _drives = new();

    [ObservableProperty]
    private ObservableCollection<DirectoryEntry> _directories = new();

    [ObservableProperty]
    private ObservableCollection<FileEntry> _files = new();

    [ObservableProperty]
    private ObservableCollection<object> _selectedItems = new();

    [ObservableProperty]
    private string _currentPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _pathHistory = new();

    [ObservableProperty]
    private int _historyIndex = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    private bool _isLoading;

    public bool IsNotLoading => !IsLoading;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private ObservableCollection<FileEntry> _searchResults = new();

    [ObservableProperty]
    private FileSortField _currentSortField = FileSortField.Name;

    [ObservableProperty]
    private bool _sortAscending = true;

    [ObservableProperty]
    private FileViewMode _viewMode = FileViewMode.Details;

    [ObservableProperty]
    private bool _showHiddenFiles;

    [ObservableProperty]
    private int _totalFileCount;

    [ObservableProperty]
    private int _totalDirectoryCount;

    [ObservableProperty]
    private long _totalSize;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private long _selectedSize;

    // 大小分析结果
    [ObservableProperty]
    private SizeAnalysisResult? _sizeAnalysisResult;

    [ObservableProperty]
    private bool _isSizeAnalyzing;

    // 类型统计结果
    [ObservableProperty]
    private TypeStatisticsResult? _typeStatisticsResult;

    [ObservableProperty]
    private bool _isTypeAnalyzing;

    // AI 分析结果
    [ObservableProperty]
    private AIAnalysisResult? _aiAnalysisResult;

    [ObservableProperty]
    private bool _isAIAnalyzing;

    [ObservableProperty]
    private string _aiAnalysisStatus = string.Empty;

    // 进度
    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private bool _showProgress;

    #endregion

    public FileManagerViewModel(
        IFileSystemService fileSystemService,
        ISizeAnalyzer sizeAnalyzer,
        ITypeStatisticsService typeStatisticsService,
        IAIFileAnalyzer aiFileAnalyzer,
        IBatchFileOperator batchFileOperator,
        IFileSearchService fileSearchService)
    {
        _fileSystemService = fileSystemService;
        _sizeAnalyzer = sizeAnalyzer;
        _typeStatisticsService = typeStatisticsService;
        _aiFileAnalyzer = aiFileAnalyzer;
        _batchFileOperator = batchFileOperator;
        _fileSearchService = fileSearchService;
    }

    /// <summary>
    /// 初始化（必须在 UI 线程调用）。
    /// </summary>
    public void Initialize()
    {
        _dispatcherQueue ??= DispatcherQueue.GetForCurrentThread();
    }

    #region 导航命令

    [RelayCommand]
    private async Task LoadDrivesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在加载驱动器...";

            var drives = await _fileSystemService.GetDrivesAsync();
            
            Drives.Clear();
            foreach (var drive in drives)
            {
                Drives.Add(drive);
            }

            StatusMessage = $"已加载 {drives.Count} 个驱动器";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载驱动器失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToAsync(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            IsLoading = true;
            StatusMessage = $"正在加载 {path}...";

            var content = await _fileSystemService.GetDirectoryContentAsync(path, new FileFilterOptions
            {
                ShowHiddenFiles = ShowHiddenFiles
            });

            // 更新历史记录
            if (CurrentPath != path)
            {
                // 如果不是从历史记录导航，则添加到历史
                if (HistoryIndex < PathHistory.Count - 1)
                {
                    // 清除前进历史
                    while (PathHistory.Count > HistoryIndex + 1)
                    {
                        PathHistory.RemoveAt(PathHistory.Count - 1);
                    }
                }
                PathHistory.Add(path);
                HistoryIndex = PathHistory.Count - 1;
            }

            CurrentPath = path;

            // 更新目录列表
            Directories.Clear();
            foreach (var dir in content.Directories)
            {
                Directories.Add(dir);
            }

            // 更新文件列表
            Files.Clear();
            foreach (var file in content.Files)
            {
                Files.Add(file);
            }

            // 应用排序
            ApplySort();

            // 更新统计
            TotalDirectoryCount = content.Directories.Count;
            TotalFileCount = content.Files.Count;
            TotalSize = content.TotalSize;

            StatusMessage = $"{TotalDirectoryCount} 个文件夹，{TotalFileCount} 个文件";
        }
        catch (UnauthorizedAccessException)
        {
            StatusMessage = "访问被拒绝";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateUpAsync()
    {
        if (string.IsNullOrEmpty(CurrentPath)) return;

        var parent = _fileSystemService.GetParentPath(CurrentPath);
        if (!string.IsNullOrEmpty(parent))
        {
            await NavigateToAsync(parent);
        }
    }

    [RelayCommand]
    private async Task NavigateBackAsync()
    {
        if (HistoryIndex > 0)
        {
            HistoryIndex--;
            var path = PathHistory[HistoryIndex];
            await NavigateToAsync(path);
        }
    }

    [RelayCommand]
    private async Task NavigateForwardAsync()
    {
        if (HistoryIndex < PathHistory.Count - 1)
        {
            HistoryIndex++;
            var path = PathHistory[HistoryIndex];
            await NavigateToAsync(path);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!string.IsNullOrEmpty(CurrentPath))
        {
            await NavigateToAsync(CurrentPath);
        }
    }

    #endregion

    #region 文件操作命令

    [RelayCommand]
    private async Task OpenItemAsync(object? item)
    {
        if (item is DirectoryEntry dir)
        {
            await NavigateToAsync(dir.FullPath);
        }
        else if (item is FileEntry file)
        {
            var result = await _fileSystemService.OpenFileAsync(file.FullPath);
            if (!result.Success)
            {
                StatusMessage = $"打开失败: {result.ErrorMessage}";
            }
        }
    }

    [RelayCommand]
    private async Task OpenInExplorerAsync()
    {
        if (SelectedItems.Count == 1)
        {
            var path = SelectedItems[0] switch
            {
                DirectoryEntry dir => dir.FullPath,
                FileEntry file => file.FullPath,
                _ => CurrentPath
            };
            await _fileSystemService.OpenInExplorerAsync(path);
        }
        else if (!string.IsNullOrEmpty(CurrentPath))
        {
            await _fileSystemService.OpenInExplorerAsync(CurrentPath);
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (SelectedItems.Count == 0) return;

        try
        {
            IsLoading = true;
            var paths = SelectedItems.Select(item => item switch
            {
                DirectoryEntry dir => dir.FullPath,
                FileEntry file => file.FullPath,
                _ => null
            }).Where(p => p != null).Cast<string>().ToList();

            var result = await _batchFileOperator.DeleteAsync(paths, true, new Progress<BatchOperationProgress>(p =>
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    ProgressValue = p.ProgressPercentage;
                    ProgressText = $"正在删除 {p.CurrentItem}...";
                });
            }));

            StatusMessage = $"已删除 {result.SuccessCount} 个项目";
            if (result.FailedCount > 0)
            {
                StatusMessage += $"，{result.FailedCount} 个失败";
            }

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            ShowProgress = false;
        }
    }

    [RelayCommand]
    private async Task CopySelectedAsync(string destinationPath)
    {
        if (SelectedItems.Count == 0 || string.IsNullOrEmpty(destinationPath)) return;

        try
        {
            IsLoading = true;
            ShowProgress = true;

            var operations = SelectedItems.Select(item =>
            {
                var sourcePath = item switch
                {
                    DirectoryEntry dir => dir.FullPath,
                    FileEntry file => file.FullPath,
                    _ => null
                };
                if (sourcePath == null) return ((string, string)?)null;
                var name = Path.GetFileName(sourcePath);
                var destPath = Path.Combine(destinationPath, name);
                return (sourcePath, destPath);
            }).Where(x => x.HasValue).Select(x => x!.Value).ToList();

            var result = await _batchFileOperator.CopyAsync(operations, false, new Progress<BatchOperationProgress>(p =>
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    ProgressValue = p.ProgressPercentage;
                    ProgressText = $"正在复制 {p.CurrentItem}...";
                });
            }));

            StatusMessage = $"已复制 {result.SuccessCount} 个项目";
        }
        catch (Exception ex)
        {
            StatusMessage = $"复制失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            ShowProgress = false;
        }
    }

    [RelayCommand]
    private async Task MoveSelectedAsync(string destinationPath)
    {
        if (SelectedItems.Count == 0 || string.IsNullOrEmpty(destinationPath)) return;

        try
        {
            IsLoading = true;
            ShowProgress = true;

            var operations = SelectedItems.Select(item =>
            {
                var sourcePath = item switch
                {
                    DirectoryEntry dir => dir.FullPath,
                    FileEntry file => file.FullPath,
                    _ => null
                };
                if (sourcePath == null) return ((string, string)?)null;
                var name = Path.GetFileName(sourcePath);
                var destPath = Path.Combine(destinationPath, name);
                return (sourcePath, destPath);
            }).Where(x => x.HasValue).Select(x => x!.Value).ToList();

            var result = await _batchFileOperator.MoveAsync(operations, false, new Progress<BatchOperationProgress>(p =>
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    ProgressValue = p.ProgressPercentage;
                    ProgressText = $"正在移动 {p.CurrentItem}...";
                });
            }));

            StatusMessage = $"已移动 {result.SuccessCount} 个项目";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"移动失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            ShowProgress = false;
        }
    }

    [RelayCommand]
    private async Task CreateFolderAsync(string folderName)
    {
        if (string.IsNullOrEmpty(CurrentPath) || string.IsNullOrEmpty(folderName)) return;

        try
        {
            var newPath = Path.Combine(CurrentPath, folderName);
            var result = await _fileSystemService.CreateDirectoryAsync(newPath);
            
            if (result.Success)
            {
                StatusMessage = $"已创建文件夹: {folderName}";
                await RefreshAsync();
            }
            else
            {
                StatusMessage = $"创建失败: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"创建失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 重命名文件或文件夹。
    /// </summary>
    public async Task RenameAsync(object item, string newName)
    {
        if (item == null || string.IsNullOrEmpty(newName)) return;

        try
        {
            var path = item switch
            {
                DirectoryEntry dir => dir.FullPath,
                FileEntry file => file.FullPath,
                _ => null
            };

            if (path == null) return;

            var result = await _fileSystemService.RenameAsync(path, newName);
            
            if (result.Success)
            {
                StatusMessage = $"已重命名为: {newName}";
                await RefreshAsync();
            }
            else
            {
                StatusMessage = $"重命名失败: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"重命名失败: {ex.Message}";
        }
    }

    #endregion

    #region 搜索命令

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrEmpty(SearchText) || string.IsNullOrEmpty(CurrentPath)) return;

        try
        {
            IsSearching = true;
            SearchResults.Clear();
            StatusMessage = $"正在搜索 \"{SearchText}\"...";

            await foreach (var file in _fileSearchService.QuickSearchAsync(CurrentPath, $"*{SearchText}*", true))
            {
                DispatcherQueue?.TryEnqueue(() => SearchResults.Add(file));
            }

            StatusMessage = $"找到 {SearchResults.Count} 个结果";
        }
        catch (Exception ex)
        {
            StatusMessage = $"搜索失败: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
        SearchResults.Clear();
        IsSearching = false;
    }

    #endregion

    #region 分析命令

    [RelayCommand]
    private async Task AnalyzeSizeAsync()
    {
        if (string.IsNullOrEmpty(CurrentPath)) return;

        try
        {
            IsSizeAnalyzing = true;
            ShowProgress = true;
            StatusMessage = "正在分析大小分布...";

            SizeAnalysisResult = await _sizeAnalyzer.AnalyzeAsync(CurrentPath, 1, new Progress<SizeAnalysisProgress>(p =>
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    ProgressText = $"正在分析: {p.CurrentPath}";
                    ProgressValue = p.ProgressPercentage;
                });
            }));

            StatusMessage = $"分析完成，总大小: {FormatSize(SizeAnalysisResult.TotalSize)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"分析失败: {ex.Message}";
        }
        finally
        {
            IsSizeAnalyzing = false;
            ShowProgress = false;
        }
    }

    [RelayCommand]
    private async Task AnalyzeTypesAsync()
    {
        if (string.IsNullOrEmpty(CurrentPath)) return;

        try
        {
            IsTypeAnalyzing = true;
            ShowProgress = true;
            StatusMessage = "正在统计文件类型...";

            TypeStatisticsResult = await _typeStatisticsService.AnalyzeAsync(CurrentPath, true, new Progress<TypeStatisticsProgress>(p =>
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    ProgressText = $"已处理 {p.ProcessedFiles} 个文件";
                });
            }));

            StatusMessage = $"统计完成，共 {TypeStatisticsResult.TotalFiles} 个文件";
        }
        catch (Exception ex)
        {
            StatusMessage = $"统计失败: {ex.Message}";
        }
        finally
        {
            IsTypeAnalyzing = false;
            ShowProgress = false;
        }
    }

    [RelayCommand]
    private async Task AnalyzeWithAIAsync()
    {
        if (string.IsNullOrEmpty(CurrentPath)) return;

        if (!_aiFileAnalyzer.IsConfigured)
        {
            StatusMessage = "请先配置 OpenAI API";
            return;
        }

        try
        {
            IsAIAnalyzing = true;
            ShowProgress = true;
            AiAnalysisStatus = "正在准备分析...";

            AiAnalysisResult = await _aiFileAnalyzer.AnalyzeAsync(CurrentPath, null, new Progress<AIAnalysisProgress>(p =>
            {
                DispatcherQueue?.TryEnqueue(() =>
                {
                    AiAnalysisStatus = p.Phase switch
                    {
                        AIAnalysisPhase.CollectingData => "正在收集文件信息...",
                        AIAnalysisPhase.PreparingRequest => "正在准备 AI 请求...",
                        AIAnalysisPhase.WaitingForResponse => "正在等待 AI 响应...",
                        AIAnalysisPhase.ParsingResponse => "正在解析 AI 响应...",
                        AIAnalysisPhase.Complete => "分析完成",
                        _ => p.StatusMessage ?? "分析中..."
                    };
                    ProgressValue = p.ProgressPercentage;
                });
            }));

            StatusMessage = $"AI 分析完成，生成 {AiAnalysisResult?.Suggestions.Count ?? 0} 条建议";
        }
        catch (Exception ex)
        {
            StatusMessage = $"AI 分析失败: {ex.Message}";
        }
        finally
        {
            IsAIAnalyzing = false;
            ShowProgress = false;
        }
    }

    [RelayCommand]
    private async Task ApplyAISuggestionsAsync()
    {
        if (AiAnalysisResult == null || AiAnalysisResult.Suggestions.Count == 0) return;

        try
        {
            IsLoading = true;
            ShowProgress = true;
            StatusMessage = "正在应用 AI 建议...";

            var result = await _batchFileOperator.ApplySuggestionsAsync(
                AiAnalysisResult.Suggestions.ToList(),
                false,
                new Progress<BatchOperationProgress>(p =>
                {
                    DispatcherQueue?.TryEnqueue(() =>
                    {
                        ProgressValue = p.ProgressPercentage;
                        ProgressText = $"正在处理: {p.CurrentItem}";
                    });
                }));

            StatusMessage = $"已应用 {result.SuccessCount} 条建议";
            if (result.FailedCount > 0)
            {
                StatusMessage += $"，{result.FailedCount} 条失败";
            }

            AiAnalysisResult = null;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"应用建议失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            ShowProgress = false;
        }
    }

    #endregion

    #region 排序和视图

    [RelayCommand]
    private void SortBy(FileSortField field)
    {
        if (CurrentSortField == field)
        {
            SortAscending = !SortAscending;
        }
        else
        {
            CurrentSortField = field;
            SortAscending = true;
        }
        ApplySort();
    }

    [RelayCommand]
    private void SetViewMode(FileViewMode mode)
    {
        ViewMode = mode;
    }

    [RelayCommand]
    private async Task ToggleHiddenFilesAsync()
    {
        ShowHiddenFiles = !ShowHiddenFiles;
        await RefreshAsync();
    }

    private void ApplySort()
    {
        var sortedFiles = CurrentSortField switch
        {
            FileSortField.Name => SortAscending
                ? Files.OrderBy(f => f.Name).ToList()
                : Files.OrderByDescending(f => f.Name).ToList(),
            FileSortField.Size => SortAscending
                ? Files.OrderBy(f => f.Size).ToList()
                : Files.OrderByDescending(f => f.Size).ToList(),
            FileSortField.Type => SortAscending
                ? Files.OrderBy(f => f.Extension).ToList()
                : Files.OrderByDescending(f => f.Extension).ToList(),
            FileSortField.ModifiedDate => SortAscending
                ? Files.OrderBy(f => f.ModifiedTime).ToList()
                : Files.OrderByDescending(f => f.ModifiedTime).ToList(),
            _ => Files.ToList()
        };

        var sortedDirs = CurrentSortField switch
        {
            FileSortField.Name => SortAscending
                ? Directories.OrderBy(d => d.Name).ToList()
                : Directories.OrderByDescending(d => d.Name).ToList(),
            FileSortField.ModifiedDate => SortAscending
                ? Directories.OrderBy(d => d.ModifiedTime).ToList()
                : Directories.OrderByDescending(d => d.ModifiedTime).ToList(),
            _ => Directories.ToList()
        };

        Files.Clear();
        foreach (var file in sortedFiles)
        {
            Files.Add(file);
        }

        Directories.Clear();
        foreach (var dir in sortedDirs)
        {
            Directories.Add(dir);
        }
    }

    #endregion

    #region 选择处理

    partial void OnSelectedItemsChanged(ObservableCollection<object> value)
    {
        UpdateSelectionInfo();
    }

    public void UpdateSelectionInfo()
    {
        SelectedCount = SelectedItems.Count;
        SelectedSize = SelectedItems.Sum(item => item switch
        {
            FileEntry file => file.Size,
            _ => 0L
        });
    }

    [RelayCommand]
    private void SelectAll()
    {
        SelectedItems.Clear();
        foreach (var dir in Directories)
        {
            SelectedItems.Add(dir);
        }
        foreach (var file in Files)
        {
            SelectedItems.Add(file);
        }
        UpdateSelectionInfo();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedItems.Clear();
        UpdateSelectionInfo();
    }

    #endregion

    #region 辅助方法

    public static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }

    #endregion
}

/// <summary>
/// 文件排序字段。
/// </summary>
public enum FileSortField
{
    Name,
    Size,
    Type,
    ModifiedDate
}

/// <summary>
/// 文件视图模式。
/// </summary>
public enum FileViewMode
{
    Details,
    List,
    Tiles,
    Icons
}
