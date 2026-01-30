using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Windows.Storage.Pickers;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;
using WinRT.Interop;

namespace WindowsSoftwareOrganizer.ViewModels;

/// <summary>
/// ViewModel for the migration page.
/// Receives software from SoftwareListPage and handles migration execution.
/// </summary>
public partial class MigrationViewModel : ObservableObject
{
    private readonly IMigrationEngine _migrationEngine;
    private readonly INamingEngine _namingEngine;
    private readonly IConfigurationService _configService;
    private CancellationTokenSource? _migrationCts;
    private DispatcherQueue? _dispatcherQueue;
    private nint _windowHandle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSoftware))]
    [NotifyPropertyChangedFor(nameof(HasNoSoftware))]
    [NotifyPropertyChangedFor(nameof(CanExecuteMigration))]
    private ObservableCollection<SoftwareEntry> _selectedSoftware = new();

    [ObservableProperty]
    private SoftwareEntry? _selectedItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanExecuteMigration))]
    [NotifyPropertyChangedFor(nameof(HasPreview))]
    private string _targetPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanExecuteMigration))]
    private NamingTemplate? _selectedTemplate;

    [ObservableProperty]
    private ObservableCollection<NamingTemplate> _templates = new();

    [ObservableProperty]
    private LinkType _selectedLinkType = LinkType.Junction;

    [ObservableProperty]
    private bool _updateRegistry = true;

    [ObservableProperty]
    private bool _verifyIntegrity = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotMigrating))]
    [NotifyPropertyChangedFor(nameof(CanExecuteMigration))]
    private bool _isMigrating;

    public bool IsNotMigrating => !IsMigrating;

    [ObservableProperty]
    private int _migrationProgress;

    [ObservableProperty]
    private string _statusMessage = "请在软件列表中选择要迁移的软件";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreview))]
    private string _previewPath = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedItems))]
    private int _selectedItemsCount;

    public bool HasSoftware => SelectedSoftware.Count > 0;
    public bool HasNoSoftware => SelectedSoftware.Count == 0;
    public bool HasPreview => !string.IsNullOrEmpty(PreviewPath);
    public bool HasSelectedItems => SelectedItemsCount > 0;
    public bool CanExecuteMigration => HasSoftware && !string.IsNullOrEmpty(TargetPath) && SelectedTemplate != null && !IsMigrating;

    public MigrationViewModel(
        IMigrationEngine migrationEngine,
        INamingEngine namingEngine,
        IConfigurationService configService)
    {
        _migrationEngine = migrationEngine;
        _namingEngine = namingEngine;
        _configService = configService;

        SelectedSoftware.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(HasSoftware));
            OnPropertyChanged(nameof(HasNoSoftware));
            OnPropertyChanged(nameof(CanExecuteMigration));
        };

        LoadSettingsAsync();
    }

    public IReadOnlyList<LinkType> LinkTypes { get; } = Enum.GetValues<LinkType>();

    /// <summary>
    /// Initializes the DispatcherQueue and window handle for UI operations.
    /// </summary>
    public void Initialize(nint windowHandle)
    {
        _dispatcherQueue ??= DispatcherQueue.GetForCurrentThread();
        _windowHandle = windowHandle;
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

    private async void LoadSettingsAsync()
    {
        try
        {
            var config = await _configService.GetConfigurationAsync();
            TargetPath = config.DefaultTargetPath;
            SelectedLinkType = config.PreferredLinkType;
            UpdateRegistry = config.AutoUpdateRegistry;

            // Load templates
            var presetTemplates = _namingEngine.GetPresetTemplates();
            Templates.Clear();
            foreach (var template in presetTemplates)
            {
                Templates.Add(template);
            }
            foreach (var template in config.CustomTemplates)
            {
                Templates.Add(template);
            }

            // Select default template
            SelectedTemplate = Templates.FirstOrDefault(t => t.Id == config.DefaultNamingTemplateId)
                ?? Templates.FirstOrDefault();
        }
        catch
        {
            StatusMessage = "加载配置失败，使用默认设置";
        }
    }

    partial void OnSelectedTemplateChanged(NamingTemplate? value)
    {
        UpdatePreview();
    }

    partial void OnTargetPathChanged(string value)
    {
        UpdatePreview();
        OnPropertyChanged(nameof(CanExecuteMigration));
    }

    partial void OnSelectedItemChanged(SoftwareEntry? value)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (SelectedItem == null || SelectedTemplate == null || string.IsNullOrEmpty(TargetPath))
        {
            PreviewPath = string.Empty;
            return;
        }

        var generatedName = _namingEngine.GenerateName(SelectedItem, SelectedTemplate.Pattern);
        PreviewPath = Path.Combine(TargetPath, generatedName);
    }

    /// <summary>
    /// Sets a software entry for migration (called from SoftwareListPage).
    /// </summary>
    public void SetSoftwareToMigrate(SoftwareEntry software)
    {
        // Add to migration list if not already there
        if (!SelectedSoftware.Any(s => s.Id == software.Id))
        {
            SelectedSoftware.Add(software);
        }
        
        // Select the software
        SelectedItem = software;
        
        StatusMessage = $"已添加 {SelectedSoftware.Count} 个软件到迁移列表";
        UpdatePreview();
    }

    [RelayCommand]
    private async Task BrowseTargetPathAsync()
    {
        try
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add("*");

            if (_windowHandle != 0)
            {
                InitializeWithWindow.Initialize(picker, _windowHandle);
            }

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                TargetPath = folder.Path;
                StatusMessage = $"目标路径: {TargetPath}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"选择文件夹失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RemoveFromMigration(SoftwareEntry? entry)
    {
        if (entry == null) return;

        var existing = SelectedSoftware.FirstOrDefault(s => s.Id == entry.Id);
        if (existing != null)
        {
            SelectedSoftware.Remove(existing);
            UpdateStatus();
        }
    }

    /// <summary>
    /// Updates the status message based on current state.
    /// </summary>
    public void UpdateStatus()
    {
        StatusMessage = SelectedSoftware.Count > 0 
            ? $"已添加 {SelectedSoftware.Count} 个软件到迁移列表"
            : "请在软件列表中选择要迁移的软件";
        
        OnPropertyChanged(nameof(HasSoftware));
        OnPropertyChanged(nameof(HasNoSoftware));
        OnPropertyChanged(nameof(CanExecuteMigration));
    }

    [RelayCommand]
    private async Task ExecuteMigrationAsync()
    {
        if (SelectedSoftware.Count == 0)
        {
            StatusMessage = "请先选择要迁移的软件";
            return;
        }

        if (string.IsNullOrEmpty(TargetPath))
        {
            StatusMessage = "请选择目标路径";
            return;
        }

        if (SelectedTemplate == null)
        {
            StatusMessage = "请选择命名模板";
            return;
        }

        _migrationCts?.Cancel();
        _migrationCts = new CancellationTokenSource();
        var token = _migrationCts.Token;

        try
        {
            IsMigrating = true;
            MigrationProgress = 0;

            var options = new MigrationOptions
            {
                LinkType = SelectedLinkType,
                UpdateRegistry = UpdateRegistry,
                VerifyIntegrity = VerifyIntegrity,
                OnFileConflict = ConflictResolution.Rename,
                OnLockedFile = LockedFileHandling.Skip
            };

            var progress = new Progress<MigrationProgress>(p =>
            {
                RunOnUIThread(() =>
                {
                    MigrationProgress = p.ProgressPercentage;
                    StatusMessage = p.CurrentFile ?? "迁移中...";
                });
            });

            // Migrate each software
            int completed = 0;
            int total = SelectedSoftware.Count;
            var failedSoftware = new List<string>();

            foreach (var software in SelectedSoftware.ToList())
            {
                if (token.IsCancellationRequested) break;

                StatusMessage = $"正在迁移 {software.Name} ({completed + 1}/{total})...";

                try
                {
                    // Create plan for this software
                    var plan = await Task.Run(async () =>
                        await _migrationEngine.CreatePlanAsync(
                            software,
                            TargetPath,
                            SelectedTemplate), token);

                    if (plan.TotalSizeBytes > plan.AvailableSpaceBytes)
                    {
                        failedSoftware.Add($"{software.Name} (空间不足)");
                        continue;
                    }

                    // Execute migration
                    var result = await Task.Run(async () =>
                        await _migrationEngine.ExecuteAsync(
                            plan,
                            options,
                            progress,
                            token), token);

                    if (!result.Success)
                    {
                        failedSoftware.Add($"{software.Name} ({result.ErrorMessage})");
                    }
                    else
                    {
                        completed++;
                    }
                }
                catch (Exception ex)
                {
                    failedSoftware.Add($"{software.Name} ({ex.Message})");
                }
            }

            if (token.IsCancellationRequested) return;

            RunOnUIThread(() =>
            {
                if (failedSoftware.Count == 0)
                {
                    StatusMessage = $"迁移完成！成功迁移 {completed} 个软件";
                    MigrationProgress = 100;
                    SelectedSoftware.Clear();
                }
                else if (completed > 0)
                {
                    StatusMessage = $"部分完成：成功 {completed} 个，失败 {failedSoftware.Count} 个";
                }
                else
                {
                    StatusMessage = $"迁移失败：{string.Join(", ", failedSoftware)}";
                }
            });
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "迁移已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"迁移出错: {ex.Message}";
        }
        finally
        {
            IsMigrating = false;
        }
    }

    [RelayCommand]
    private void CancelMigration()
    {
        _migrationCts?.Cancel();
        StatusMessage = "迁移已取消";
        MigrationProgress = 0;
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
