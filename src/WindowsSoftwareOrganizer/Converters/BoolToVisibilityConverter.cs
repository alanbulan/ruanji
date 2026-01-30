using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace WindowsSoftwareOrganizer.Converters;

/// <summary>
/// 将布尔值转换为 Visibility 枚举
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// 是否反转转换逻辑
    /// </summary>
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            if (Invert)
                boolValue = !boolValue;
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility visibility)
        {
            var result = visibility == Visibility.Visible;
            return Invert ? !result : result;
        }
        return false;
    }
}
