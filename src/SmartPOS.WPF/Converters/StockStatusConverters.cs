using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SmartPOS.WPF.Converters
{
    /// <summary>
    /// Returns a background brush color based on stock quantity.
    /// Green for in-stock, orange for low stock, red for out-of-stock.
    /// </summary>
    public class StockBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int qty)
            {
                if (qty <= 0)   return new SolidColorBrush(Color.FromRgb(0xFE, 0xE2, 0xE2)); // red-100
                if (qty <= 5)   return new SolidColorBrush(Color.FromRgb(0xFF, 0xED, 0xC6)); // orange-100
                return              new SolidColorBrush(Color.FromRgb(0xD1, 0xFA, 0xE5)); // green-100
            }
            return new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Returns a foreground brush color based on stock quantity.
    /// </summary>
    public class StockForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int qty)
            {
                if (qty <= 0)   return new SolidColorBrush(Color.FromRgb(0x99, 0x1B, 0x1B)); // red-800
                if (qty <= 5)   return new SolidColorBrush(Color.FromRgb(0x92, 0x40, 0x0E)); // orange-800
                return              new SolidColorBrush(Color.FromRgb(0x06, 0x5F, 0x46)); // green-800
            }
            return new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Returns a display label for stock quantity.
    /// </summary>
    public class StockTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int qty)
            {
                if (qty <= 0) return "Hết hàng";
                if (qty <= 5) return $"Sắp hết ({qty})";
                return $"Còn {qty}";
            }
            return "N/A";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }
}
