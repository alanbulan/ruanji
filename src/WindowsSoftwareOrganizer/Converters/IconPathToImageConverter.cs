using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace WindowsSoftwareOrganizer.Converters;

/// <summary>
/// Converts an icon path to a BitmapImage for display.
/// Note: Icon loading is handled in code-behind via ContainerContentChanging event.
/// </summary>
public class IconPathToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        // Icon loading is handled in SoftwareListPage.xaml.cs
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
