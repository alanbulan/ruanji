using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.ViewModels;

/// <summary>
/// ViewModel for the cleanup page.
/// Uses Singleton pattern to persist state across page navigation.
/// </summary>
public partial class CleanupViewModel : ObservableObject
{
    private readonly ICleanupEngine _cleanupEngine;
    private CancellationTokenSource? _scanCts;
    private CancellationTokenSource? _cleanupCts;
    private DispatcherQueue? _dispatcherQueue;

    [ObservableProperty]
    private ObservableCollection<CleanupItemViewModel> _cleanupItems = new();

    [ObservableProperty]
    private ObservableCollection<CleanupItemViewModel> _selectedItems = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotScanning))]
    private bool _isScanning;

    public bool IsNotScanning => !IsScanning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotCleaning))]
    private bool _isCleaning;

    public bool IsNotCleaning => !IsCleaning;

    [ObservableProperty]
    private int _scanProgress;

    [ObservableProperty]
    private int _cleanupProgress;

    [ObservableProperty]
    private string _statusMessage = "点击扫描按钮开始扫描";

    [ObservableProperty]
    private bool _moveToRecycleBin = true;

    [ObservableProperty]
    private long _totalSelectedSize;

    [ObservableProperty]
    private string _selectedSizeText = "0 B";

    public CleanupViewModel(ICleanupEngine cleanupEngine)
    {
        _cleanupEngine = cleanupEngine;
    }

    /// <summary>
    /// Initializes the DispatcherQueue for UI thread operations.
    /// Must be called from the UI thread.
    /// </summary>
    public void InitializeDispatcher()
    {
        _dispatcherQueue ??= DispatcherQueue.GetForCurrentThread();
    }

    private void RunOnUIThread(Action action)
    {
        if (_dispatcherQueue == null)
        {
            action();
            return;
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            action();
        }
        else
        {
            _dispatcherQueue.TryEnqueue(() => action());
        }
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        // Cancel any existing scan
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;

        try
        {
            IsScanning = true;
            ScanProgress = 0;
            StatusMessage = "正在扫描残留目录...";
            
            RunOnUIThread(() => CleanupItems.Clear());

            // Run scan on background thread
            var orphanedItems = await Task.Run(async () => 
                await _cleanupEngine.ScanOrphanedItemsAsync(token), token);
            
            if (token.IsCancellationRequested) return;
            
            RunOnUIThread(() => ScanProgress = 40);
            StatusMessage = "正在扫描缓存目录...";

            var cacheItems = await Task.Run(async () => 
                await _cleanupEngine.ScanCacheAsync(null, token), token);
            
            if (token.IsCancellationRequested) return;
            
            RunOnUIThread(() => ScanProgress = 100);

            // Add items on UI thread
            RunOnUIThread(() =>
            {
                foreach (var item in orphanedItems.Concat(cacheItems))
                {
                    CleanupItems.Add(new CleanupItemViewModel(item));
                }
                StatusMessage = $"扫描完成，发现 {CleanupItems.Count} 个可清理项目";
                UpdateSelectedSize();
            });
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "扫描已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"扫描失败: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        _scanCts?.Cancel();
        StatusMessage = "正在取消扫描...";
    }

    [RelayCommand]
    private async Task CleanupAsync()
    {
        var itemsToClean = CleanupItems.Where(i => i.IsSelected).ToList();
        
        if (itemsToClean.Count == 0)
        {
            StatusMessage = "请先选择要清理的项目";
            return;
        }

        // Cancel any existing cleanup
        _cleanupCts?.Cancel();
        _cleanupCts = new CancellationTokenSource();
        var token = _cleanupCts.Token;

        try
        {
            IsCleaning = true;
            CleanupProgress = 0;
            StatusMessage = "正在清理...";

            var progress = new Progress<CleanupProgress>(p =>
            {
                RunOnUIThread(() =>
                {
                    CleanupProgress = p.ProgressPercentage;
                    StatusMessage = p.StatusMessage ?? "清理中...";
                });
            });

            var result = await Task.Run(async () =>
                await _cleanupEngine.CleanupAsync(
                    itemsToClean.Select(i => i.Item),
                    MoveToRecycleBin,
                    progress,
                    token), token);

            if (token.IsCancellationRequested) return;

            RunOnUIThread(() =>
            {
                if (result.Success)
                {
                    StatusMessage = $"清理完成！释放 {FormatSize(result.BytesFreed)}，清理 {result.ItemsCleaned} 个项目";
                    
                    // Remove cleaned items from the list
                    foreach (var item in itemsToClean.ToList())
                    {
                        CleanupItems.Remove(item);
                    }
                }
                else
                {
                    StatusMessage = $"清理部分失败: {result.ErrorMessage}";
                }

                UpdateSelectedSize();
            });
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "清理已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"清理出错: {ex.Message}";
        }
        finally
        {
            IsCleaning = false;
            CleanupProgress = 100;
        }
    }

    [RelayCommand]
    private void CancelCleanup()
    {
        _cleanupCts?.Cancel();
        StatusMessage = "正在取消清理...";
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in CleanupItems)
        {
            item.IsSelected = true;
        }
        UpdateSelectedSize();
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var item in CleanupItems)
        {
            item.IsSelected = false;
        }
        UpdateSelectedSize();
    }

    [RelayCommand]
    private void SelectSafe()
    {
        foreach (var item in CleanupItems)
        {
            item.IsSelected = item.Item.Risk == RiskLevel.Safe;
        }
        UpdateSelectedSize();
    }

    public void UpdateSelectedSize()
    {
        TotalSelectedSize = CleanupItems.Where(i => i.IsSelected).Sum(i => i.Item.SizeBytes);
        SelectedSizeText = FormatSize(TotalSelectedSize);
    }

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
}

/// <summary>
/// ViewModel wrapper for CleanupItem with selection state.
/// </summary>
public partial class CleanupItemViewModel : ObservableObject
{
    public CleanupItem Item { get; }

    [ObservableProperty]
    private bool _isSelected;

    public CleanupItemViewModel(CleanupItem item)
    {
        Item = item;
        // Auto-select safe items
        IsSelected = item.Risk == RiskLevel.Safe;
    }

    public string SizeText => CleanupViewModel.FormatSize(Item.SizeBytes);

    public string TypeText => Item.Type switch
    {
        CleanupItemType.OrphanedDirectory => "残留目录",
        CleanupItemType.OrphanedRegistryKey => "残留注册表",
        CleanupItemType.CacheDirectory => "缓存目录",
        CleanupItemType.TempFile => "临时文件",
        CleanupItemType.LogFile => "日志文件",
        _ => "未知"
    };

    public string RiskText => Item.Risk switch
    {
        RiskLevel.Safe => "安全",
        RiskLevel.Caution => "谨慎",
        RiskLevel.Dangerous => "危险",
        _ => "未知"
    };
}
