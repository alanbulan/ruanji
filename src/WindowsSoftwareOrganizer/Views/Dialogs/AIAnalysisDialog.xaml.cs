using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Views.Dialogs;

/// <summary>
/// AI 分析结果对话框。
/// </summary>
public sealed partial class AIAnalysisDialog : ContentDialog, INotifyPropertyChanged
{
    public AIAnalysisResult Result { get; }
    
    public ObservableCollection<OrganizationSuggestion> Suggestions { get; }

    private int _selectedCount;
    public int SelectedCount
    {
        get => _selectedCount;
        set
        {
            if (_selectedCount != value)
            {
                _selectedCount = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AIAnalysisDialog(AIAnalysisResult result)
    {
        Result = result;
        Suggestions = new ObservableCollection<OrganizationSuggestion>(result.Suggestions);
        UpdateSelectedCount();
        this.InitializeComponent();
    }

    private void UpdateSelectedCount()
    {
        SelectedCount = Suggestions.Count(s => s.IsSelected);
    }

    private void SuggestionCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.Tag is string id)
        {
            var suggestion = Suggestions.FirstOrDefault(s => s.Id == id);
            if (suggestion != null)
            {
                suggestion.IsSelected = checkBox.IsChecked ?? false;
                UpdateSelectedCount();
            }
        }
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var suggestion in Suggestions)
        {
            suggestion.IsSelected = true;
        }
        // Refresh the list to update UI
        var items = Suggestions.ToList();
        Suggestions.Clear();
        foreach (var item in items)
        {
            Suggestions.Add(item);
        }
        UpdateSelectedCount();
    }

    private void DeselectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var suggestion in Suggestions)
        {
            suggestion.IsSelected = false;
        }
        // Refresh the list to update UI
        var items = Suggestions.ToList();
        Suggestions.Clear();
        foreach (var item in items)
        {
            Suggestions.Add(item);
        }
        UpdateSelectedCount();
    }

    /// <summary>
    /// 获取选中的建议列表。
    /// </summary>
    public IReadOnlyList<OrganizationSuggestion> GetSelectedSuggestions()
    {
        return Suggestions.Where(s => s.IsSelected).ToList();
    }

    /// <summary>
    /// 获取建议类型文本。
    /// </summary>
    public static string GetSuggestionTypeText(SuggestionType type)
    {
        return type switch
        {
            SuggestionType.Move => "移动",
            SuggestionType.Rename => "重命名",
            SuggestionType.Delete => "删除",
            SuggestionType.CreateFolder => "新建文件夹",
            SuggestionType.Merge => "合并",
            SuggestionType.Archive => "归档",
            _ => "未知"
        };
    }

    /// <summary>
    /// 获取优先级文本。
    /// </summary>
    public static string GetPriorityText(SuggestionPriority priority)
    {
        return priority switch
        {
            SuggestionPriority.High => "高",
            SuggestionPriority.Medium => "中",
            SuggestionPriority.Low => "低",
            _ => "未知"
        };
    }

    /// <summary>
    /// 获取优先级背景色。
    /// </summary>
    public static Brush GetPriorityBackground(SuggestionPriority priority)
    {
        return priority switch
        {
            SuggestionPriority.High => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 53, 69)),
            SuggestionPriority.Medium => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 193, 7)),
            SuggestionPriority.Low => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 108, 117, 125)),
            _ => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 108, 117, 125))
        };
    }

    /// <summary>
    /// 检查是否有目标路径。
    /// </summary>
    public static Visibility HasDestination(string? destinationPath)
    {
        return string.IsNullOrEmpty(destinationPath) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
