using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.Helpers;
using WindowsSoftwareOrganizer.ViewModels;
using WindowsSoftwareOrganizer.Views.Dialogs;

namespace WindowsSoftwareOrganizer.Views;

/// <summary>
/// Page displaying the list of installed software.
/// </summary>
public sealed partial class SoftwareListPage : Page
{
    public SoftwareListViewModel ViewModel { get; }
    private readonly IAIAssistant _aiAssistant;

    public SoftwareListPage()
    {
        ViewModel = App.Current.GetService<SoftwareListViewModel>();
        _aiAssistant = App.Current.GetService<IAIAssistant>();
        ViewModel.Initialize();
        this.InitializeComponent();
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
            Module = AIModule.SoftwareList,
            SelectedSoftware = ViewModel.FilteredList.ToList()
        };

        // 使用独立窗口而不是 ContentDialog
        var aiWindow = new AIAssistantWindow(_aiAssistant, context);
        aiWindow.Activate();
    }

    /// <summary>
    /// Handles container content changing to load software icons asynchronously.
    /// Uses phased loading for better performance.
    /// </summary>
    private async void SoftwareListView_ContainerContentChanging(
        ListViewBase sender, 
        ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue)
        {
            // Clear the image when recycling to prevent stale icons
            ClearIconFromContainer(args.ItemContainer);
            return;
        }

        if (args.Phase == 0)
        {
            // Phase 0: Show placeholder, register for phase 1
            args.RegisterUpdateCallback(SoftwareListView_ContainerContentChanging);
            return;
        }

        if (args.Phase == 1 && args.Item is SoftwareEntry entry)
        {
            await LoadIconForEntry(args.ItemContainer, entry);
        }
    }

    /// <summary>
    /// Clears the icon from a recycled container.
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
    /// Loads the icon for a software entry into its container.
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

        // Determine icon source path
        var iconPath = GetIconPath(entry);
        
        if (string.IsNullOrEmpty(iconPath))
        {
            ShowFallbackIcon(image, fallbackIcon);
            return;
        }

        try
        {
            // Use IconExtractor with Shell API for reliable extraction
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
    /// Gets the best icon path for a software entry.
    /// </summary>
    private static string? GetIconPath(SoftwareEntry entry)
    {
        // Priority: IconPath > ExecutablePath
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
    /// Shows the fallback icon when extraction fails.
    /// </summary>
    private static void ShowFallbackIcon(Image image, FontIcon? fallbackIcon)
    {
        image.Source = null;
        if (fallbackIcon != null)
            fallbackIcon.Visibility = Visibility.Visible;
    }
}
