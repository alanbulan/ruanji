using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Helpers;
using WindowsSoftwareOrganizer.ViewModels;
using WindowsSoftwareOrganizer.Views.Dialogs;
using WinRT.Interop;

namespace WindowsSoftwareOrganizer.Views;

/// <summary>
/// 软件迁移页面
/// 接收来自软件列表页面的软件，执行迁移操作
/// </summary>
public sealed partial class MigrationPage : Page
{
    public MigrationViewModel ViewModel { get; }
    private readonly IAIAssistant _aiAssistant;

    public MigrationPage()
    {
        ViewModel = App.Current.GetService<MigrationViewModel>();
        _aiAssistant = App.Current.GetService<IAIAssistant>();
        this.InitializeComponent();
        
        // 初始化窗口句柄用于文件夹选择器
        var window = App.Current.MainWindow;
        if (window != null)
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            ViewModel.Initialize(hwnd);
        }
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
            Module = AIModule.Migration,
            SelectedSoftware = ViewModel.SelectedSoftware.ToList(),
            MigrationTargetPath = ViewModel.TargetPath
        };

        // 使用独立窗口
        var aiWindow = new AIAssistantWindow(_aiAssistant, context);
        aiWindow.Activate();
    }

    /// <summary>
    /// 设置要迁移的软件（从 MainWindow 导航时调用）
    /// </summary>
    public void SetSoftwareToMigrate(SoftwareEntry software)
    {
        ViewModel.SetSoftwareToMigrate(software);
    }

    private void SoftwareListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelectedItemsCount = SoftwareListView.SelectedItems.Count;
    }

    private void RemoveSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItems = SoftwareListView.SelectedItems.Cast<SoftwareEntry>().ToList();
        foreach (var item in selectedItems)
        {
            ViewModel.SelectedSoftware.Remove(item);
        }
        ViewModel.UpdateStatus();
    }

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedSoftware.Clear();
        ViewModel.UpdateStatus();
    }


    /// <summary>
    /// 处理容器内容变化以异步加载软件图标
    /// 使用分阶段加载以提高性能
    /// </summary>
    private async void SoftwareListView_ContainerContentChanging(
        ListViewBase sender, 
        ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue)
        {
            // 回收时清除图像以防止显示旧图标
            ClearIconFromContainer(args.ItemContainer);
            return;
        }

        if (args.Phase == 0)
        {
            // 阶段 0：显示占位符，注册阶段 1
            args.RegisterUpdateCallback(SoftwareListView_ContainerContentChanging);
            return;
        }

        if (args.Phase == 1 && args.Item is SoftwareEntry entry)
        {
            await LoadIconForEntry(args.ItemContainer, entry);
        }
    }

    /// <summary>
    /// 清除回收容器中的图标
    /// </summary>
    private static void ClearIconFromContainer(SelectorItem container)
    {
        if (container.ContentTemplateRoot is not Grid templateRoot)
            return;

        var iconBorder = templateRoot.Children[0] as Border;
        if (iconBorder?.Child is not Grid iconGrid)
            return;

        var image = iconGrid.Children.OfType<Image>().FirstOrDefault();
        if (image != null)
        {
            image.Source = null;
        }

        var fallbackIcon = iconGrid.Children.OfType<FontIcon>().FirstOrDefault();
        if (fallbackIcon != null)
        {
            fallbackIcon.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// 为软件条目加载图标到其容器中
    /// </summary>
    private static async Task LoadIconForEntry(SelectorItem container, SoftwareEntry entry)
    {
        if (container.ContentTemplateRoot is not Grid templateRoot)
            return;

        var iconBorder = templateRoot.Children[0] as Border;
        if (iconBorder?.Child is not Grid iconGrid)
            return;

        var image = iconGrid.Children.OfType<Image>().FirstOrDefault();
        var fallbackIcon = iconGrid.Children.OfType<FontIcon>().FirstOrDefault();

        if (image == null)
            return;

        // 确定图标源路径
        var iconPath = GetIconPath(entry);
        
        if (string.IsNullOrEmpty(iconPath))
        {
            ShowFallbackIcon(image, fallbackIcon);
            return;
        }

        try
        {
            // 使用 IconExtractor 通过 Shell API 可靠地提取图标
            var bitmap = await IconExtractor.ExtractIconAsync(iconPath);
            
            if (bitmap != null)
            {
                image.Source = bitmap;
                if (fallbackIcon != null)
                    fallbackIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                ShowFallbackIcon(image, fallbackIcon);
            }
        }
        catch
        {
            ShowFallbackIcon(image, fallbackIcon);
        }
    }

    /// <summary>
    /// 获取软件条目的最佳图标路径
    /// </summary>
    private static string? GetIconPath(SoftwareEntry entry)
    {
        // 优先级：IconPath > ExecutablePath
        if (!string.IsNullOrEmpty(entry.IconPath) && File.Exists(entry.IconPath))
        {
            return entry.IconPath;
        }

        if (!string.IsNullOrEmpty(entry.ExecutablePath) && File.Exists(entry.ExecutablePath))
        {
            return entry.ExecutablePath;
        }

        return null;
    }

    /// <summary>
    /// 当提取失败时显示备用图标
    /// </summary>
    private static void ShowFallbackIcon(Image image, FontIcon? fallbackIcon)
    {
        image.Source = null;
        if (fallbackIcon != null)
            fallbackIcon.Visibility = Visibility.Visible;
    }
}
