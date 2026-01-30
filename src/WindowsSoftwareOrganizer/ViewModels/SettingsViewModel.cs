using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.ViewModels;

/// <summary>
/// ViewModel for the settings page.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfigurationService _configService;
    private readonly INamingEngine _namingEngine;
    private readonly IOperationLogger _operationLogger;
    private readonly IOpenAIClient _openAIClient;

    [ObservableProperty]
    private string _defaultTargetPath = string.Empty;

    [ObservableProperty]
    private LinkType _preferredLinkType = LinkType.Junction;

    [ObservableProperty]
    private bool _autoUpdateRegistry = true;

    [ObservableProperty]
    private string _defaultNamingTemplateId = string.Empty;

    [ObservableProperty]
    private ObservableCollection<NamingTemplate> _templates = new();

    [ObservableProperty]
    private NamingTemplate? _selectedTemplate;

    [ObservableProperty]
    private ObservableCollection<OperationRecord> _operationHistory = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ElementTheme _currentTheme = ElementTheme.Default;

    // OpenAI 配置
    [ObservableProperty]
    private string _openAIApiKey = string.Empty;

    [ObservableProperty]
    private string _openAIBaseUrl = "https://api.openai.com/v1";

    [ObservableProperty]
    private string _openAIModel = string.Empty;

    [ObservableProperty]
    private bool _openAIEnabled;

    [ObservableProperty]
    private bool _isTestingAPI;

    [ObservableProperty]
    private string _apiTestResult = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ModelInfo> _availableModels = new();

    [ObservableProperty]
    private bool _isLoadingModels;

    [ObservableProperty]
    private string _modelsLoadStatus = string.Empty;

    // 标记是否有未保存的更改
    private bool _hasUnsavedChanges;
    private bool _isInitializing = true;

    public IReadOnlyList<LinkType> LinkTypes { get; } = Enum.GetValues<LinkType>();

    public IReadOnlyList<ElementTheme> Themes { get; } = new[]
    {
        ElementTheme.Default,
        ElementTheme.Light,
        ElementTheme.Dark
    };

    public SettingsViewModel(
        IConfigurationService configService,
        INamingEngine namingEngine,
        IOperationLogger operationLogger,
        IOpenAIClient openAIClient)
    {
        _configService = configService;
        _namingEngine = namingEngine;
        _operationLogger = operationLogger;
        _openAIClient = openAIClient;

        LoadSettingsAsync();
    }

    private async void LoadSettingsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在加载设置...";

            var config = await _configService.GetConfigurationAsync();
            
            DefaultTargetPath = config.DefaultTargetPath;
            PreferredLinkType = config.PreferredLinkType;
            AutoUpdateRegistry = config.AutoUpdateRegistry;
            DefaultNamingTemplateId = config.DefaultNamingTemplateId;

            // Load OpenAI settings
            OpenAIApiKey = config.OpenAIConfiguration.ApiKey;
            OpenAIBaseUrl = config.OpenAIConfiguration.BaseUrl;
            OpenAIModel = config.OpenAIConfiguration.Model;
            OpenAIEnabled = config.OpenAIConfiguration.IsEnabled;

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

            SelectedTemplate = Templates.FirstOrDefault(t => t.Id == DefaultNamingTemplateId)
                ?? Templates.FirstOrDefault();

            // Load operation history
            await LoadOperationHistoryAsync();

            StatusMessage = "设置已加载";
            _isInitializing = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载设置失败: {ex.Message}";
            _isInitializing = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 属性变更时标记有未保存的更改。
    /// </summary>
    partial void OnDefaultTargetPathChanged(string value) => MarkAsChanged();
    partial void OnPreferredLinkTypeChanged(LinkType value) => MarkAsChanged();
    partial void OnAutoUpdateRegistryChanged(bool value) => MarkAsChanged();
    partial void OnOpenAIApiKeyChanged(string value) => MarkAsChanged();
    partial void OnOpenAIBaseUrlChanged(string value) => MarkAsChanged();
    partial void OnOpenAIModelChanged(string value) => MarkAsChanged();
    partial void OnOpenAIEnabledChanged(bool value) => MarkAsChanged();
    partial void OnSelectedTemplateChanged(NamingTemplate? value) => MarkAsChanged();

    private void MarkAsChanged()
    {
        if (!_isInitializing)
        {
            _hasUnsavedChanges = true;
        }
    }

    /// <summary>
    /// 页面离开时自动保存设置。
    /// </summary>
    public async Task SaveIfChangedAsync()
    {
        if (_hasUnsavedChanges && !_isInitializing)
        {
            await SaveSettingsAsync();
        }
    }

    private async Task LoadOperationHistoryAsync()
    {
        var since = DateTime.Now.AddDays(-30);
        var history = await _operationLogger.GetHistoryAsync(since, 50);
        OperationHistory.Clear();
        foreach (var record in history)
        {
            OperationHistory.Add(record);
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在保存设置...";

            var config = new AppConfiguration
            {
                DefaultTargetPath = DefaultTargetPath,
                PreferredLinkType = PreferredLinkType,
                AutoUpdateRegistry = AutoUpdateRegistry,
                DefaultNamingTemplateId = SelectedTemplate?.Id ?? string.Empty,
                CustomTemplates = Templates
                    .Where(t => !_namingEngine.GetPresetTemplates().Any(p => p.Id == t.Id))
                    .ToList(),
                OpenAIConfiguration = new OpenAIConfiguration
                {
                    ApiKey = OpenAIApiKey,
                    BaseUrl = OpenAIBaseUrl,
                    Model = OpenAIModel,
                    IsEnabled = OpenAIEnabled
                }
            };

            await _configService.SaveConfigurationAsync(config);
            
            // Update OpenAI client configuration
            _openAIClient.Configure(config.OpenAIConfiguration);
            
            _hasUnsavedChanges = false;
            StatusMessage = "设置已保存";
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存设置失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task BrowseTargetPathAsync()
    {
        try
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add("*");

            // Get the window handle for WinUI 3
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                DefaultTargetPath = folder.Path;
                StatusMessage = $"已选择: {folder.Path}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"选择文件夹失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshHistoryAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在刷新历史记录...";
            await LoadOperationHistoryAsync();
            StatusMessage = $"已加载 {OperationHistory.Count} 条历史记录";
        }
        catch (Exception ex)
        {
            StatusMessage = $"刷新历史记录失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SetTheme(ElementTheme theme)
    {
        CurrentTheme = theme;
        
        // Apply theme to the app
        if (App.Current.MainWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }
    }

    [RelayCommand]
    private async Task TestAPIConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(OpenAIApiKey))
        {
            ApiTestResult = "请先输入 API 密钥";
            return;
        }

        try
        {
            IsTestingAPI = true;
            ApiTestResult = "正在测试连接...";

            // Create temporary configuration for testing
            var testConfig = new OpenAIConfiguration
            {
                ApiKey = OpenAIApiKey,
                BaseUrl = OpenAIBaseUrl,
                Model = OpenAIModel,
                IsEnabled = true
            };

            var result = await _openAIClient.TestConnectionAsync(testConfig);

            if (result.Success)
            {
                ApiTestResult = $"连接成功！响应时间: {result.ResponseTimeMs}ms，模型: {result.Model}";
            }
            else
            {
                ApiTestResult = $"连接失败: {result.Message}\n{result.ErrorDetails}";
            }
        }
        catch (Exception ex)
        {
            ApiTestResult = $"测试失败: {ex.Message}";
        }
        finally
        {
            IsTestingAPI = false;
        }
    }

    [RelayCommand]
    private async Task LoadModelsAsync()
    {
        if (string.IsNullOrWhiteSpace(OpenAIApiKey) || string.IsNullOrWhiteSpace(OpenAIBaseUrl))
        {
            ModelsLoadStatus = "请先输入 API 密钥和地址";
            return;
        }

        try
        {
            IsLoadingModels = true;
            ModelsLoadStatus = "正在获取模型列表...";

            var testConfig = new OpenAIConfiguration
            {
                ApiKey = OpenAIApiKey,
                BaseUrl = OpenAIBaseUrl,
                IsEnabled = true
            };

            var models = await _openAIClient.GetAvailableModelsAsync(testConfig);

            AvailableModels.Clear();
            
            if (models.Count > 0)
            {
                foreach (var model in models)
                {
                    AvailableModels.Add(model);
                }
                ModelsLoadStatus = $"已加载 {models.Count} 个模型";
                
                // 如果当前没有选择模型，选择第一个
                if (string.IsNullOrEmpty(OpenAIModel) && models.Count > 0)
                {
                    OpenAIModel = models[0].Id;
                }
            }
            else
            {
                ModelsLoadStatus = "未获取到模型，请手动输入模型名称";
            }
        }
        catch (Exception ex)
        {
            ModelsLoadStatus = $"获取失败: {ex.Message}";
        }
        finally
        {
            IsLoadingModels = false;
        }
    }

    public static string GetThemeDisplayName(ElementTheme theme) => theme switch
    {
        ElementTheme.Default => "跟随系统",
        ElementTheme.Light => "浅色",
        ElementTheme.Dark => "深色",
        _ => "未知"
    };

    public static string GetOperationTypeDisplayName(OperationType type) => type switch
    {
        OperationType.Migration => "迁移",
        OperationType.Cleanup => "清理",
        OperationType.Rollback => "回滚",
        _ => "未知"
    };

    public static string FormatDateTime(DateTime dateTime) => dateTime.ToString("yyyy-MM-dd HH:mm:ss");
}
