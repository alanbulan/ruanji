using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WindowsSoftwareOrganizer.Core.Interfaces;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Views.Dialogs;

/// <summary>
/// OpenAI API 配置对话框。
/// </summary>
public sealed partial class OpenAIConfigDialog : ContentDialog
{
    private readonly IOpenAIClient _openAIClient;

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";

    public OpenAIConfigDialog(IOpenAIClient openAIClient, OpenAIConfiguration? currentConfig = null)
    {
        _openAIClient = openAIClient;
        
        if (currentConfig != null)
        {
            ApiKey = currentConfig.ApiKey ?? string.Empty;
            BaseUrl = currentConfig.BaseUrl ?? string.Empty;
            Model = currentConfig.Model ?? "gpt-4o-mini";
        }
        
        this.InitializeComponent();
    }

    /// <summary>
    /// 获取配置结果。
    /// </summary>
    public OpenAIConfiguration GetConfiguration()
    {
        return new OpenAIConfiguration
        {
            ApiKey = ApiKey,
            BaseUrl = string.IsNullOrWhiteSpace(BaseUrl) ? "https://api.openai.com/v1" : BaseUrl,
            Model = Model
        };
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        TestButton.IsEnabled = false;
        TestResultPanel.Visibility = Visibility.Visible;
        TestResultIcon.Glyph = "\uE895"; // Loading
        TestResultIcon.Foreground = new SolidColorBrush(Colors.Gray);
        TestResultText.Text = "正在测试连接...";

        try
        {
            // 临时配置
            var config = GetConfiguration();
            _openAIClient.UpdateConfiguration(config);

            var result = await _openAIClient.TestConnectionAsync();

            if (result.Success)
            {
                TestResultIcon.Glyph = "\uE73E"; // Checkmark
                TestResultIcon.Foreground = new SolidColorBrush(Colors.Green);
                TestResultText.Text = $"连接成功！模型: {result.Model}";
            }
            else
            {
                TestResultIcon.Glyph = "\uE711"; // Error
                TestResultIcon.Foreground = new SolidColorBrush(Colors.Red);
                TestResultText.Text = $"连接失败: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            TestResultIcon.Glyph = "\uE711"; // Error
            TestResultIcon.Foreground = new SolidColorBrush(Colors.Red);
            TestResultText.Text = $"测试出错: {ex.Message}";
        }
        finally
        {
            TestButton.IsEnabled = true;
        }
    }
}
