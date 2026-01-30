using Microsoft.UI.Xaml.Data;

namespace WindowsSoftwareOrganizer.Converters;

/// <summary>
/// Converts byte size to human-readable string format.
/// </summary>
public class SizeToStringConverter : IValueConverter
{
    private static readonly string[] SizeUnits = { "B", "KB", "MB", "GB", "TB" };

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is long bytes)
        {
            return FormatSize(bytes);
        }
        if (value is int intBytes)
        {
            return FormatSize(intBytes);
        }
        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0)
            return "-";

        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < SizeUnits.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {SizeUnits[order]}";
    }
}
