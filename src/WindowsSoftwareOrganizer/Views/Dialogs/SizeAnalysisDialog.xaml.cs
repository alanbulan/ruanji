using Microsoft.UI.Xaml.Controls;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Views.Dialogs;

/// <summary>
/// 大小分析结果对话框。
/// </summary>
public sealed partial class SizeAnalysisDialog : ContentDialog
{
    public SizeAnalysisResult Result { get; }

    public SizeAnalysisDialog(SizeAnalysisResult result)
    {
        Result = result;
        this.InitializeComponent();
    }

    /// <summary>
    /// 获取项目图标。
    /// </summary>
    public static string GetItemIcon(bool isDirectory)
    {
        return isDirectory ? "\uE8B7" : "\uE8A5";
    }
}
