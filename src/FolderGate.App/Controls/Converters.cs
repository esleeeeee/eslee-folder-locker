using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FolderGate.App.Controls;

/// <summary>
/// Converter instances for XAML (x:Static), usable without application
/// resources so windows still load in tests.
/// </summary>
public static class Converters
{
    public static readonly IValueConverter BoolToVisibility = new BoolToVisibilityConverter(invert: false);

    public static readonly IValueConverter InverseBoolToVisibility = new BoolToVisibilityConverter(invert: true);

    private sealed class BoolToVisibilityConverter(bool invert) : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is true;
            return flag ^ invert ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
