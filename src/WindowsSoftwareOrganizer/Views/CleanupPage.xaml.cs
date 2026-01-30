using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.ViewModels;
using WindowsSoftwareOrganizer.Views.Dialogs;

namespace WindowsSoftwareOrganizer.Views;

/// <summary>
/// Page for managing cleanup operations.
/// </summary>
public sealed partial class CleanupPage : Page
{
    public CleanupViewModel ViewModel { get; }
    private readonly IAIAssistant _aiAssistant;

    public CleanupPage()
    {
        ViewModel = App.Current.GetService<CleanupViewModel>();
        _aiAssistant = App.Current.GetService<IAIAssistant>();
        this.InitializeComponent();
        
        // Initialize dispatcher for UI thread operations
        ViewModel.InitializeDispatcher();
        // AI 按钮始终显示，点击时检查配置状态
    }

    private async void AIAssistant_Click(object sender, RoutedEventArgs e)
    {
        // 异步确保配置已加载
        var isConfigured = await _aiAssistant.EnsureConfiguredAsync();
        
        if (!isConfigured)
        {
            var dialog = new ContentDialog
            {
                Title = "AI 未配置",
                Content = "请先在设置页面配置 AI API 密钥。",
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            return;
        }

        var context = new AIAssistantContext
        {
            Module = AIModule.Cleanup,
            CleanupItems = ViewModel.CleanupItems.Select(i => i.Item).ToList()
        };

        // 使用独立窗口
        var aiWindow = new AIAssistantWindow(_aiAssistant, context);
        aiWindow.Activate();
    }

    private void CheckBox_Checked(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateSelectedSize();
    }

    private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        ViewModel.UpdateSelectedSize();
    }
}
