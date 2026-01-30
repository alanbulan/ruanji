using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.Win32;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.ViewModels;

/// <summary>
/// ViewModel for the software list page.
/// </summary>
public partial class SoftwareListViewModel : ObservableObject
{
    private readonly ISoftwareScanner _scanner;
    private readonly ISoftwareClassifier _classifier;
    private DispatcherQueue? _dispatcherQueue;

    // 确保在 UI 线程上获取 DispatcherQueue
    private DispatcherQueue DispatcherQueue => _dispatcherQueue ??= DispatcherQueue.GetForCurrentThread();

    [ObservableProperty]
    private ObservableCollection<SoftwareEntry> _softwareList = new();

    [ObservableProperty]
    private ObservableCollection<SoftwareEntry> _filteredList = new();

    [ObservableProperty]
    private SoftwareEntry? _selectedSoftware;

    // 记录当前正在处理卸载的软件ID
    private string? _uninstallingSoftwareId;

    partial void OnSelectedSoftwareChanged(SoftwareEntry? value)
    {
        IsDetailsPanelOpen = value != null;
        
        // 切换软件时清空残留项（除非是刚卸载完的软件）
        if (value != null && value.Id != _uninstallingSoftwareId)
        {
            LeftoverItems.Clear();
            HasLeftovers = false;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    private bool _isLoading;

    public bool IsNotLoading => !IsLoading;

    [ObservableProperty]
    private bool _isDetailsPanelOpen;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private SoftwareCategory? _selectedCategory;

    [ObservableProperty]
    private SortField _currentSortField = SortField.Name;

    [ObservableProperty]
    private bool _sortAscending = true;

    [ObservableProperty]
    private int _scanProgress;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private ObservableCollection<string> _leftoverItems = new();

    [ObservableProperty]
    private bool _hasLeftovers;

    public SoftwareListViewModel(ISoftwareScanner scanner, ISoftwareClassifier classifier)
    {
        _scanner = scanner;
        _classifier = classifier;
        _scanner.ProgressChanged += OnScanProgressChanged;
        
        // 初始化默认选中"所有类别"
        SelectedCategoryOption = Categories[0];
    }

    /// <summary>
    /// 初始化 DispatcherQueue（必须在 UI 线程调用）
    /// </summary>
    public void Initialize()
    {
        _dispatcherQueue ??= DispatcherQueue.GetForCurrentThread();
    }


    public IReadOnlyList<CategoryOption> Categories { get; } = 
        new CategoryOption[] { new(null, "所有类别") }
        .Concat(Enum.GetValues<SoftwareCategory>().Select(c => new CategoryOption(c, CategoryToDisplayName(c))))
        .ToList();

    [ObservableProperty]
    private CategoryOption? _selectedCategoryOption;

    partial void OnSelectedCategoryOptionChanged(CategoryOption? value)
    {
        SelectedCategory = value?.Category;
        ApplyFilterAndSort();
    }

    private static string CategoryToDisplayName(SoftwareCategory category)
    {
        return category switch
        {
            SoftwareCategory.Game => "游戏",
            SoftwareCategory.Social => "社交通讯",
            SoftwareCategory.Messaging => "即时通讯",
            SoftwareCategory.Office => "办公软件",
            SoftwareCategory.Browser => "浏览器",
            SoftwareCategory.Music => "音乐播放",
            SoftwareCategory.Video => "视频播放",
            SoftwareCategory.Media => "影音娱乐",
            SoftwareCategory.Graphics => "图形设计",
            SoftwareCategory.Photo => "图片处理",
            SoftwareCategory.Modeling3D => "3D建模",
            SoftwareCategory.System => "系统工具",
            SoftwareCategory.Security => "安全软件",
            SoftwareCategory.Antivirus => "杀毒软件",
            SoftwareCategory.Download => "下载工具",
            SoftwareCategory.Network => "网络工具",
            SoftwareCategory.VPN => "VPN工具",
            SoftwareCategory.Education => "教育学习",
            SoftwareCategory.Driver => "驱动程序",
            SoftwareCategory.Runtime => "运行库",
            SoftwareCategory.IDE => "开发环境",
            SoftwareCategory.CodeEditor => "代码编辑",
            SoftwareCategory.SDK => "开发套件",
            SoftwareCategory.DevTool => "开发工具",
            SoftwareCategory.VersionControl => "版本控制",
            SoftwareCategory.Database => "数据库",
            SoftwareCategory.Virtualization => "虚拟化",
            SoftwareCategory.Utility => "实用工具",
            SoftwareCategory.Compression => "压缩解压",
            SoftwareCategory.FileManager => "文件管理",
            SoftwareCategory.Backup => "备份恢复",
            SoftwareCategory.RemoteDesktop => "远程控制",
            SoftwareCategory.Screenshot => "截图录屏",
            SoftwareCategory.Notes => "笔记软件",
            SoftwareCategory.Reader => "阅读器",
            SoftwareCategory.Ebook => "电子书",
            SoftwareCategory.Translation => "翻译工具",
            SoftwareCategory.InputMethod => "输入法",
            SoftwareCategory.CloudStorage => "云存储",
            SoftwareCategory.Email => "邮件客户端",
            SoftwareCategory.Finance => "财务软件",
            SoftwareCategory.Health => "健康健身",
            SoftwareCategory.Weather => "天气",
            SoftwareCategory.Maps => "地图导航",
            SoftwareCategory.Shopping => "购物",
            SoftwareCategory.News => "新闻资讯",
            SoftwareCategory.Streaming => "直播平台",
            SoftwareCategory.AI => "AI工具",
            SoftwareCategory.Other => "其他",
            _ => category.ToString()
        };
    }

    private void OnScanProgressChanged(object? sender, ScanProgressEventArgs e)
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            ScanProgress = e.ProgressPercentage;
            StatusMessage = e.StatusMessage ?? "扫描中...";
        });
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在扫描已安装软件...";
            ScanProgress = 0;

            IReadOnlyList<SoftwareEntry> entries;
            try
            {
                entries = await _scanner.ScanInstalledSoftwareAsync();
            }
            catch (Exception scanEx)
            {
                StatusMessage = $"扫描失败: {scanEx.Message}";
                return;
            }
            
            SoftwareList.Clear();
            
            // Process entries in batches to avoid UI freeze
            var total = entries.Count;
            var processed = 0;
            
            foreach (var entry in entries)
            {
                try
                {
                    // Apply classification (quick operation)
                    var category = _classifier.Classify(entry);
                    
                    // Skip slow FindRelatedDirectories for now
                    var classifiedEntry = entry with { Category = category };
                    SoftwareList.Add(classifiedEntry);
                }
                catch
                {
                    // If classification fails, add the entry as-is
                    SoftwareList.Add(entry);
                }
                
                processed++;
                if (processed % 10 == 0)
                {
                    StatusMessage = $"正在处理 {processed}/{total}...";
                    // Allow UI to update
                    await Task.Delay(1);
                }
            }

            ApplyFilterAndSort();
            StatusMessage = $"扫描完成，共发现 {SoftwareList.Count} 个软件";
        }
        catch (Exception ex)
        {
            StatusMessage = $"扫描失败: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            ScanProgress = 100;
        }
    }

    [RelayCommand]
    private void SelectSoftware(SoftwareEntry? entry)
    {
        SelectedSoftware = entry;
        IsDetailsPanelOpen = entry != null;
    }

    [RelayCommand]
    private void CloseDetailsPanel()
    {
        IsDetailsPanelOpen = false;
        SelectedSoftware = null;
        StatusMessage = $"共 {FilteredList.Count} 个软件";
    }

    [RelayCommand]
    private void SortBy(SortField field)
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
        ApplyFilterAndSort();
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (SelectedSoftware == null || string.IsNullOrEmpty(SelectedSoftware.InstallPath))
            return;

        var path = SelectedSoftware.InstallPath;
        if (path == "未知路径" || !Directory.Exists(path))
        {
            StatusMessage = "无法打开：路径不存在";
            return;
        }

        try
        {
            Process.Start("explorer.exe", path);
            StatusMessage = $"已打开: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开文件夹失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task UninstallSoftwareAsync()
    {
        if (SelectedSoftware == null)
            return;

        var softwareToUninstall = SelectedSoftware;
        _uninstallingSoftwareId = softwareToUninstall.Id;

        try
        {
            // 清空之前的残留项
            LeftoverItems.Clear();
            HasLeftovers = false;
            
            StatusMessage = $"正在卸载 {softwareToUninstall.Name}...";
            
            // Get uninstall string from registry
            var uninstallString = GetUninstallString(softwareToUninstall.Id);
            if (string.IsNullOrEmpty(uninstallString))
            {
                StatusMessage = "未找到卸载程序";
                _uninstallingSoftwareId = null;
                return;
            }

            // Run uninstaller
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {uninstallString}",
                UseShellExecute = true
            };
            
            var process = Process.Start(startInfo);
            if (process != null)
            {
                StatusMessage = $"卸载程序已启动，请完成卸载后点击确认...";
                
                // 等待卸载程序退出
                await process.WaitForExitAsync();
                
                // 额外等待一下，让系统完成清理
                await Task.Delay(1000);
                
                // 检查软件是否真的被卸载了（注册表中是否还存在）
                var stillExists = GetUninstallString(softwareToUninstall.Id) != null;
                
                if (stillExists)
                {
                    StatusMessage = $"卸载程序已关闭，但 {softwareToUninstall.Name} 可能未完全卸载";
                }
                else
                {
                    StatusMessage = $"{softwareToUninstall.Name} 已卸载，正在扫描残留...";
                }
                
                // 扫描残留（无论是否完全卸载都扫描）
                await ScanLeftoversForSoftwareAsync(softwareToUninstall);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"卸载失败: {ex.Message}";
            _uninstallingSoftwareId = null;
        }
    }

    // 为特定软件扫描残留（不依赖 SelectedSoftware）
    private async Task ScanLeftoversForSoftwareAsync(SoftwareEntry software)
    {
        var leftovers = new List<string>();
        var softwareName = software.Name;
        var installPath = software.InstallPath;
        var vendor = software.Vendor;

        // Generate search keywords from software name and vendor
        var searchKeywords = GenerateSearchKeywords(softwareName, vendor);

        await Task.Run(() =>
        {
            // 1. Check if install folder still exists
            if (installPath != "未知路径" && Directory.Exists(installPath))
            {
                var size = GetDirectorySize(installPath);
                leftovers.Add($"[安装目录] {installPath} ({FormatSize(size)})");
            }

            // 2. Scan AppData locations (Roaming, Local, LocalLow, ProgramData)
            ScanAppDataLocations(searchKeywords, leftovers);

            // 3. Scan Program Files directories
            ScanProgramFilesLocations(searchKeywords, leftovers, installPath);

            // 4. Scan user profile folders (Documents, Desktop, etc.)
            ScanUserProfileLocations(searchKeywords, leftovers);

            // 5. Scan Temp folders
            ScanTempLocations(searchKeywords, leftovers);

            // 6. Scan Start Menu shortcuts
            ScanStartMenuLocations(searchKeywords, leftovers);

            // 7. Scan registry for leftovers (more comprehensive)
            ScanRegistryLeftovers(searchKeywords, leftovers);

            // 8. Scan Windows Tasks
            ScanScheduledTasks(searchKeywords, leftovers);

            // 9. Scan Services
            ScanServices(searchKeywords, leftovers);
        });

        DispatcherQueue?.TryEnqueue(() =>
        {
            // 只有当前选中的软件是刚卸载的软件时才显示残留
            if (SelectedSoftware?.Id == software.Id || _uninstallingSoftwareId == software.Id)
            {
                LeftoverItems.Clear();
                foreach (var item in leftovers)
                {
                    LeftoverItems.Add(item);
                }
                HasLeftovers = leftovers.Count > 0;
                StatusMessage = leftovers.Count > 0 
                    ? $"发现 {leftovers.Count} 个残留项" 
                    : "未发现残留文件";
            }
            _uninstallingSoftwareId = null;
        });
    }

    private string? GetUninstallString(string softwareId)
    {
        string[] registryPaths = new[]
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        foreach (var basePath in registryPaths)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"{basePath}\{softwareId}");
                if (key != null)
                {
                    var uninstallString = key.GetValue("UninstallString") as string;
                    if (!string.IsNullOrEmpty(uninstallString))
                        return uninstallString;
                }
            }
            catch { }

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey($@"{basePath}\{softwareId}");
                if (key != null)
                {
                    var uninstallString = key.GetValue("UninstallString") as string;
                    if (!string.IsNullOrEmpty(uninstallString))
                        return uninstallString;
                }
            }
            catch { }
        }

        return null;
    }

    private List<string> GenerateSearchKeywords(string softwareName, string? vendor)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Add full name
        keywords.Add(softwareName);
        
        // Add name without version numbers
        var nameWithoutVersion = System.Text.RegularExpressions.Regex.Replace(
            softwareName, @"\s*[\d\.]+\s*$", "").Trim();
        if (!string.IsNullOrEmpty(nameWithoutVersion))
            keywords.Add(nameWithoutVersion);
        
        // Add name parts (split by space)
        var parts = softwareName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1)
        {
            // Add first word if it's meaningful (>3 chars)
            if (parts[0].Length > 3)
                keywords.Add(parts[0]);
            
            // Add first two words combined
            if (parts.Length >= 2)
                keywords.Add($"{parts[0]}{parts[1]}");
        }
        
        // Add vendor if available
        if (!string.IsNullOrEmpty(vendor))
        {
            keywords.Add(vendor);
            // Common vendor name variations
            var vendorParts = vendor.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (vendorParts.Length > 0 && vendorParts[0].Length > 3)
                keywords.Add(vendorParts[0]);
        }
        
        // Remove common words that would cause false positives
        var commonWords = new[] { "the", "inc", "inc.", "corp", "corp.", "ltd", "ltd.", 
            "software", "application", "app", "program", "tool", "tools", "free", "pro" };
        keywords.RemoveWhere(k => commonWords.Contains(k.ToLowerInvariant()) || k.Length < 3);
        
        return keywords.ToList();
    }

    private void ScanAppDataLocations(List<string> keywords, List<string> leftovers)
    {
        var appDataPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow"),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
        };

        foreach (var basePath in appDataPaths)
        {
            if (!Directory.Exists(basePath)) continue;
            ScanDirectoryForKeywords(basePath, keywords, leftovers, "AppData", 2);
        }
    }

    private void ScanProgramFilesLocations(List<string> keywords, List<string> leftovers, string installPath)
    {
        var programFilesPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86)
        };

        foreach (var basePath in programFilesPaths)
        {
            if (!Directory.Exists(basePath)) continue;
            // Skip if this is the install path we already found
            ScanDirectoryForKeywords(basePath, keywords, leftovers, "程序文件", 1, installPath);
        }
    }

    private void ScanUserProfileLocations(List<string> keywords, List<string> leftovers)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var userPaths = new[]
        {
            Path.Combine(userProfile, "Documents"),
            Path.Combine(userProfile, "Desktop"),
            Path.Combine(userProfile, "Downloads"),
            Path.Combine(userProfile, ".config"),
            Path.Combine(userProfile, "Saved Games")
        };

        foreach (var basePath in userPaths)
        {
            if (!Directory.Exists(basePath)) continue;
            ScanDirectoryForKeywords(basePath, keywords, leftovers, "用户文件", 1);
        }
    }

    private void ScanTempLocations(List<string> keywords, List<string> leftovers)
    {
        var tempPaths = new[]
        {
            Path.GetTempPath(),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")
        };

        foreach (var basePath in tempPaths)
        {
            if (!Directory.Exists(basePath)) continue;
            ScanDirectoryForKeywords(basePath, keywords, leftovers, "临时文件", 1);
        }
    }

    private void ScanStartMenuLocations(List<string> keywords, List<string> leftovers)
    {
        var startMenuPaths = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "Microsoft", "Windows", "Start Menu", "Programs"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), 
                "Microsoft", "Windows", "Start Menu", "Programs")
        };

        foreach (var basePath in startMenuPaths)
        {
            if (!Directory.Exists(basePath)) continue;
            ScanDirectoryForKeywords(basePath, keywords, leftovers, "开始菜单", 2);
            
            // Also scan for .lnk files
            try
            {
                foreach (var file in Directory.GetFiles(basePath, "*.lnk", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (keywords.Any(k => fileName.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    {
                        leftovers.Add($"[快捷方式] {file}");
                    }
                }
            }
            catch { }
        }
    }

    private void ScanDirectoryForKeywords(string basePath, List<string> keywords, 
        List<string> leftovers, string category, int maxDepth, string? excludePath = null)
    {
        try
        {
            foreach (var dir in Directory.GetDirectories(basePath))
            {
                if (excludePath != null && dir.Equals(excludePath, StringComparison.OrdinalIgnoreCase))
                    continue;

                var dirName = Path.GetFileName(dir);
                if (keywords.Any(k => dirName.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    var size = GetDirectorySize(dir);
                    leftovers.Add($"[{category}] {dir} ({FormatSize(size)})");
                }
                else if (maxDepth > 1)
                {
                    // Scan subdirectories
                    ScanDirectoryForKeywords(dir, keywords, leftovers, category, maxDepth - 1, excludePath);
                }
            }
        }
        catch { }
    }

    private void ScanRegistryLeftovers(List<string> keywords, List<string> leftovers)
    {
        // Registry paths to scan
        var registryLocations = new (RegistryKey root, string path, string displayRoot)[]
        {
            (Registry.CurrentUser, @"SOFTWARE", "HKCU\\SOFTWARE"),
            (Registry.LocalMachine, @"SOFTWARE", "HKLM\\SOFTWARE"),
            (Registry.LocalMachine, @"SOFTWARE\WOW6432Node", "HKLM\\SOFTWARE\\WOW6432Node"),
            (Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "HKCU\\Run"),
            (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "HKLM\\Run"),
            (Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts", "HKCU\\FileExts"),
            (Registry.ClassesRoot, "", "HKCR")
        };

        foreach (var (root, path, displayRoot) in registryLocations)
        {
            try
            {
                using var key = string.IsNullOrEmpty(path) ? root : root.OpenSubKey(path);
                if (key == null) continue;

                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    if (keywords.Any(k => subKeyName.Contains(k, StringComparison.OrdinalIgnoreCase)))
                    {
                        var fullPath = string.IsNullOrEmpty(path) 
                            ? $"{displayRoot}\\{subKeyName}" 
                            : $"{displayRoot}\\{subKeyName}";
                        leftovers.Add($"[注册表] {fullPath}");
                    }
                }
            }
            catch { }
        }
    }

    private void ScanScheduledTasks(List<string> keywords, List<string> leftovers)
    {
        try
        {
            var taskFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), 
                "System32", "Tasks");
            if (!Directory.Exists(taskFolder)) return;

            foreach (var file in Directory.GetFiles(taskFolder, "*", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                if (keywords.Any(k => fileName.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    leftovers.Add($"[计划任务] {fileName}");
                }
            }
        }
        catch { }
    }

    private void ScanServices(List<string> keywords, List<string> leftovers)
    {
        try
        {
            using var servicesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
            if (servicesKey == null) return;

            foreach (var serviceName in servicesKey.GetSubKeyNames())
            {
                if (keywords.Any(k => serviceName.Contains(k, StringComparison.OrdinalIgnoreCase)))
                {
                    leftovers.Add($"[服务] {serviceName}");
                }
            }
        }
        catch { }
    }

    private static long GetDirectorySize(string path)
    {
        try
        {
            return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                .Sum(f => new FileInfo(f).Length);
        }
        catch
        {
            return 0;
        }
    }

    [RelayCommand]
    private async Task CleanLeftoversAsync()
    {
        if (!HasLeftovers || LeftoverItems.Count == 0)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "正在清理残留...";
            var cleaned = 0;
            var failed = 0;

            foreach (var item in LeftoverItems.ToList())
            {
                try
                {
                    if (item.StartsWith("[安装目录]") || item.StartsWith("[AppData]") || 
                        item.StartsWith("[程序文件]") || item.StartsWith("[用户文件]") ||
                        item.StartsWith("[临时文件]") || item.StartsWith("[开始菜单]"))
                    {
                        // Extract path (remove category and size info)
                        var pathMatch = System.Text.RegularExpressions.Regex.Match(item, @"\] (.+?)( \(|$)");
                        if (pathMatch.Success)
                        {
                            var path = pathMatch.Groups[1].Value.Trim();
                            if (Directory.Exists(path))
                            {
                                Directory.Delete(path, true);
                                cleaned++;
                            }
                        }
                    }
                    else if (item.StartsWith("[快捷方式]"))
                    {
                        var path = item.Replace("[快捷方式] ", "");
                        if (File.Exists(path))
                        {
                            File.Delete(path);
                            cleaned++;
                        }
                    }
                    else if (item.StartsWith("[注册表]"))
                    {
                        // Registry cleanup - need admin rights
                        // For now, just count as failed
                        failed++;
                    }
                    else if (item.StartsWith("[计划任务]") || item.StartsWith("[服务]"))
                    {
                        // These require special handling and admin rights
                        failed++;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    failed++;
                }
                catch (Exception)
                {
                    failed++;
                }
            }

            LeftoverItems.Clear();
            HasLeftovers = false;
            
            var message = $"已清理 {cleaned} 个残留项";
            if (failed > 0)
                message += $"，{failed} 个需要管理员权限";
            StatusMessage = message;
            
            // Refresh the list
            await ScanAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"清理失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void MigrateSoftware()
    {
        if (SelectedSoftware == null)
        {
            StatusMessage = "请先选择要迁移的软件";
            return;
        }

        if (SelectedSoftware.InstallPath == "未知路径" || !Directory.Exists(SelectedSoftware.InstallPath))
        {
            StatusMessage = "无法迁移：安装路径不存在";
            return;
        }

        // Navigate to migration page with the selected software
        // For now, just show a message - the actual navigation will be handled by MainWindow
        StatusMessage = $"准备迁移 {SelectedSoftware.Name}...";
        
        // Raise an event or use a messenger to notify MainWindow to navigate
        MigrationRequested?.Invoke(this, SelectedSoftware);
    }

    /// <summary>
    /// Event raised when migration is requested for a software entry.
    /// </summary>
    public event EventHandler<SoftwareEntry>? MigrationRequested;

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilterAndSort();
    }

    private void ApplyFilterAndSort()
    {
        var filtered = SoftwareList.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(s => 
                s.Name.ToLowerInvariant().Contains(search) ||
                (s.Vendor?.ToLowerInvariant().Contains(search) ?? false) ||
                s.InstallPath.ToLowerInvariant().Contains(search));
        }

        // Apply category filter
        if (SelectedCategory.HasValue)
        {
            filtered = filtered.Where(s => s.Category == SelectedCategory.Value);
        }

        // Apply sorting
        filtered = CurrentSortField switch
        {
            SortField.Name => SortAscending 
                ? filtered.OrderBy(s => s.Name) 
                : filtered.OrderByDescending(s => s.Name),
            SortField.Category => SortAscending 
                ? filtered.OrderBy(s => s.Category) 
                : filtered.OrderByDescending(s => s.Category),
            SortField.Size => SortAscending 
                ? filtered.OrderBy(s => s.TotalSizeBytes) 
                : filtered.OrderByDescending(s => s.TotalSizeBytes),
            SortField.Location => SortAscending 
                ? filtered.OrderBy(s => s.InstallPath) 
                : filtered.OrderByDescending(s => s.InstallPath),
            _ => filtered
        };

        FilteredList.Clear();
        foreach (var item in filtered)
        {
            FilteredList.Add(item);
        }
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

public enum SortField
{
    Name,
    Category,
    Size,
    Location
}

/// <summary>
/// 类别选项，用于 ComboBox 显示
/// </summary>
public record CategoryOption(SoftwareCategory? Category, string DisplayName)
{
    public override string ToString() => DisplayName;
}
