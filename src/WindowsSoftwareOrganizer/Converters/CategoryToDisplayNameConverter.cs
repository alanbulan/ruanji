using Microsoft.UI.Xaml.Data;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Converters;

/// <summary>
/// Converts SoftwareCategory enum to Chinese display name.
/// </summary>
public class CategoryToDisplayNameConverter : IValueConverter
{
    private static readonly Dictionary<SoftwareCategory, string> CategoryNames = new()
    {
        [SoftwareCategory.Game] = "游戏",
        [SoftwareCategory.Social] = "社交通讯",
        [SoftwareCategory.Messaging] = "即时通讯",
        [SoftwareCategory.Office] = "办公软件",
        [SoftwareCategory.Browser] = "浏览器",
        [SoftwareCategory.Music] = "音乐播放",
        [SoftwareCategory.Video] = "视频播放",
        [SoftwareCategory.Media] = "影音娱乐",
        [SoftwareCategory.Graphics] = "图形设计",
        [SoftwareCategory.Photo] = "图片处理",
        [SoftwareCategory.Modeling3D] = "3D建模",
        [SoftwareCategory.System] = "系统工具",
        [SoftwareCategory.Security] = "安全软件",
        [SoftwareCategory.Antivirus] = "杀毒软件",
        [SoftwareCategory.Download] = "下载工具",
        [SoftwareCategory.Network] = "网络工具",
        [SoftwareCategory.VPN] = "VPN工具",
        [SoftwareCategory.Education] = "教育学习",
        [SoftwareCategory.Driver] = "驱动程序",
        [SoftwareCategory.Runtime] = "运行库",
        [SoftwareCategory.IDE] = "开发环境",
        [SoftwareCategory.CodeEditor] = "代码编辑",
        [SoftwareCategory.SDK] = "开发套件",
        [SoftwareCategory.DevTool] = "开发工具",
        [SoftwareCategory.VersionControl] = "版本控制",
        [SoftwareCategory.Database] = "数据库",
        [SoftwareCategory.Virtualization] = "虚拟化",
        [SoftwareCategory.Utility] = "实用工具",
        [SoftwareCategory.Compression] = "压缩解压",
        [SoftwareCategory.FileManager] = "文件管理",
        [SoftwareCategory.Backup] = "备份恢复",
        [SoftwareCategory.RemoteDesktop] = "远程控制",
        [SoftwareCategory.Screenshot] = "截图录屏",
        [SoftwareCategory.Notes] = "笔记软件",
        [SoftwareCategory.Reader] = "阅读器",
        [SoftwareCategory.Ebook] = "电子书",
        [SoftwareCategory.Translation] = "翻译工具",
        [SoftwareCategory.InputMethod] = "输入法",
        [SoftwareCategory.CloudStorage] = "云存储",
        [SoftwareCategory.Email] = "邮件客户端",
        [SoftwareCategory.Finance] = "财务软件",
        [SoftwareCategory.Health] = "健康健身",
        [SoftwareCategory.Weather] = "天气",
        [SoftwareCategory.Maps] = "地图导航",
        [SoftwareCategory.Shopping] = "购物",
        [SoftwareCategory.News] = "新闻资讯",
        [SoftwareCategory.Streaming] = "直播平台",
        [SoftwareCategory.AI] = "AI工具",
        [SoftwareCategory.Other] = "其他"
    };

    public static string GetDisplayName(SoftwareCategory category)
    {
        return CategoryNames.TryGetValue(category, out var name) ? name : category.ToString();
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
        {
            return "所有类别";
        }
        if (value is SoftwareCategory category)
        {
            return GetDisplayName(category);
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
