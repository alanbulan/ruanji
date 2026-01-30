using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;
using WindowsSoftwareOrganizer.ViewModels;
using WindowsSoftwareOrganizer.Views.Dialogs;

namespace WindowsSoftwareOrganizer.Views;

/// <summary>
/// 文件管理页面。
/// </summary>
public sealed partial class FileManagerPage : Page
{
    public FileManagerViewModel ViewModel { get; }
    private readonly IAIAssistant _aiAssistant;

    public FileManagerPage()
    {
        ViewModel = App.Current.GetService<FileManagerViewModel>();
        _aiAssistant = App.Current.GetService<IAIAssistant>();
        
        this.InitializeComponent();
        this.Loaded += FileManagerPage_Loaded;
        
        // 订阅分析完成事件
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private async void FileManagerPage_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Initialize();
        await ViewModel.LoadDrivesCommand.ExecuteAsync(null);
    }

    private async void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // 当分析结果更新时显示对话框
        if (e.PropertyName == nameof(ViewModel.TypeStatisticsResult) && ViewModel.TypeStatisticsResult != null)
        {
            await ShowTypeStatisticsDialogAsync();
        }
        else if (e.PropertyName == nameof(ViewModel.SizeAnalysisResult) && ViewModel.SizeAnalysisResult != null)
        {
            await ShowSizeAnalysisDialogAsync();
        }
        // AI 分析现在使用交互式对话框，不再监听 AiAnalysisResult
    }

    private async Task ShowTypeStatisticsDialogAsync()
    {
        if (ViewModel.TypeStatisticsResult == null) return;
        
        try
        {
            var dialog = new TypeStatisticsDialog(ViewModel.TypeStatisticsResult)
            {
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"显示统计对话框失败: {ex.Message}";
        }
    }

    private async Task ShowSizeAnalysisDialogAsync()
    {
        if (ViewModel.SizeAnalysisResult == null) return;
        
        try
        {
            var dialog = new SizeAnalysisDialog(ViewModel.SizeAnalysisResult)
            {
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            ViewModel.StatusMessage = $"显示大小分析对话框失败: {ex.Message}";
        }
    }

    private void PathTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            var textBox = sender as TextBox;
            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {
                ViewModel.NavigateToCommand.Execute(textBox.Text);
            }
        }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ViewModel.SearchCommand.Execute(null);
    }

    private void DriveListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is DriveEntry drive)
        {
            ViewModel.NavigateToCommand.Execute(drive.RootPath);
        }
    }

    #region 目录交互

    private void DirectoriesListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        // 单击选中
        if (e.ClickedItem is DirectoryEntry dir)
        {
            ViewModel.SelectedItems.Clear();
            ViewModel.SelectedItems.Add(dir);
            ViewModel.UpdateSelectionInfo();
        }
    }

    private void DirectoriesListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        // 双击进入目录
        if (DirectoriesListView.SelectedItem is DirectoryEntry dir)
        {
            ViewModel.NavigateToCommand.Execute(dir.FullPath);
        }
    }

    private void DirectoriesListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var element = e.OriginalSource as FrameworkElement;
        var dir = FindDataContext<DirectoryEntry>(element);
        if (dir != null)
        {
            ViewModel.SelectedItems.Clear();
            ViewModel.SelectedItems.Add(dir);
            ViewModel.UpdateSelectionInfo();
            DirectoriesListView.SelectedItem = dir;
            ShowContextMenu(DirectoriesListView, e.GetPosition(DirectoriesListView), dir);
        }
    }

    #endregion

    #region 文件交互

    private void FilesListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is FileEntry file)
        {
            ViewModel.SelectedItems.Clear();
            ViewModel.SelectedItems.Add(file);
            ViewModel.UpdateSelectionInfo();
        }
    }

    private void FilesListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        // 双击打开文件
        if (FilesListView.SelectedItem is FileEntry file)
        {
            ViewModel.OpenItemCommand.Execute(file);
        }
    }

    private void FilesListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        var element = e.OriginalSource as FrameworkElement;
        var file = FindDataContext<FileEntry>(element);
        if (file != null)
        {
            ViewModel.SelectedItems.Clear();
            ViewModel.SelectedItems.Add(file);
            ViewModel.UpdateSelectionInfo();
            FilesListView.SelectedItem = file;
            ShowContextMenu(FilesListView, e.GetPosition(FilesListView), file);
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 从元素向上查找指定类型的 DataContext。
    /// </summary>
    private T? FindDataContext<T>(FrameworkElement? element) where T : class
    {
        while (element != null)
        {
            if (element.DataContext is T data)
            {
                return data;
            }
            element = element.Parent as FrameworkElement;
        }
        return null;
    }

    private void ShowContextMenu(UIElement target, Windows.Foundation.Point position, object item)
    {
        var menu = new MenuFlyout();

        var openItem = new MenuFlyoutItem
        {
            Text = "打开",
            Icon = new FontIcon { Glyph = "\uE8E5" }
        };
        openItem.Click += (s, e) => ViewModel.OpenItemCommand.Execute(item);
        menu.Items.Add(openItem);

        var openExplorerItem = new MenuFlyoutItem
        {
            Text = "在资源管理器中打开",
            Icon = new FontIcon { Glyph = "\uE838" }
        };
        openExplorerItem.Click += (s, e) => ViewModel.OpenInExplorerCommand.Execute(null);
        menu.Items.Add(openExplorerItem);

        menu.Items.Add(new MenuFlyoutSeparator());

        // 重命名
        var renameItem = new MenuFlyoutItem
        {
            Text = "重命名",
            Icon = new FontIcon { Glyph = "\uE8AC" }
        };
        renameItem.Click += async (s, e) => await ShowRenameDialogAsync(item);
        menu.Items.Add(renameItem);

        // 删除
        var deleteItem = new MenuFlyoutItem
        {
            Text = "删除",
            Icon = new FontIcon { Glyph = "\uE74D" }
        };
        deleteItem.Click += (s, e) => ViewModel.DeleteSelectedCommand.Execute(null);
        menu.Items.Add(deleteItem);

        menu.ShowAt(target, position);
    }

    private async Task ShowRenameDialogAsync(object item)
    {
        var currentName = item switch
        {
            DirectoryEntry dir => dir.Name,
            FileEntry file => file.Name,
            _ => string.Empty
        };

        var dialog = new ContentDialog
        {
            Title = "重命名",
            PrimaryButtonText = "确定",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var textBox = new TextBox
        {
            Text = currentName,
            PlaceholderText = "输入新名称"
        };
        textBox.SelectAll();
        dialog.Content = textBox;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(textBox.Text))
        {
            await ViewModel.RenameAsync(item, textBox.Text);
        }
    }

    private async void NewFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "新建文件夹",
            PrimaryButtonText = "创建",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var textBox = new TextBox
        {
            Text = "新建文件夹",
            PlaceholderText = "输入文件夹名称"
        };
        textBox.SelectAll();
        dialog.Content = textBox;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(textBox.Text))
        {
            await ViewModel.CreateFolderCommand.ExecuteAsync(textBox.Text);
        }
    }

    private async void AIOrganize_Click(object sender, RoutedEventArgs e)
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

        if (string.IsNullOrEmpty(ViewModel.CurrentPath))
        {
            ViewModel.StatusMessage = "请先选择一个目录";
            return;
        }

        // 使用独立窗口
        var context = new AIAssistantContext
        {
            Module = AIModule.FileManager,
            CurrentPath = ViewModel.CurrentPath
        };

        var aiWindow = new AIAssistantWindow(_aiAssistant, context);
        aiWindow.Closed += async (s, args) =>
        {
            // 窗口关闭后刷新目录
            await ViewModel.RefreshCommand.ExecuteAsync(null);
        };
        aiWindow.Activate();
    }

    #endregion
}
