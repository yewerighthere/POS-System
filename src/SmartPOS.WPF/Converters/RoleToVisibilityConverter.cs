using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SmartPOS.WPF.Converters;

public class RoleToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var role = value as string;
        var inverse = parameter as string == "Inverse";

        bool isStaff = string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase);

        if (inverse)
        {
            return isStaff ? Visibility.Visible : Visibility.Collapsed;
        }

        return isStaff ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
