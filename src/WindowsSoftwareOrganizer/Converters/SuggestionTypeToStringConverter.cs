using Microsoft.UI.Xaml.Data;
using WindowsSoftwareOrganizer.Core.Models;

namespace WindowsSoftwareOrganizer.Converters;

/// <summary>
/// 将建议类型转换为显示字符串。
/// </summary>
public class SuggestionTypeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is SuggestionType type)
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
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
