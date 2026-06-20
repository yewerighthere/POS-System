using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartPOS.WPF.Converters;

public class DecimalToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is decimal d && d != 0m ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
