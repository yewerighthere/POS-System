using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SmartPOS.WPF.Converters;

/// <summary>
/// Converts a local file path (ImagePath) to a BitmapImage for display in WPF Image controls.
/// Falls back to null (shows nothing / placeholder) if path is invalid or file not found.
/// </summary>
public class LocalImagePathConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            // Loại bỏ hoàn toàn hình ảnh url internet
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Chuyển đổi đường dẫn tương đối thành WPF Pack URI
            // Định dạng Pack URI cho Resource: pack://application:,,,/SmartPOS.WPF;component/Assets/Images/filename.jpg
            string resourcePath = path;
            if (!path.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
            {
                // Chuẩn hoá đường dẫn, thay dấu '\' bằng '/'
                string normalized = path.Replace('\\', '/');
                if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                {
                    resourcePath = $"pack://application:,,,/SmartPOS.WPF;component/{normalized}";
                }
                else
                {
                    resourcePath = $"pack://application:,,,/SmartPOS.WPF;component/Assets/Images/{normalized}";
                }
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(resourcePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 200; // Limit memory usage
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converts ImagePath string: returns Visibility.Visible if path exists, Collapsed if null/empty.
/// Used to show/hide the image preview and the placeholder icon.
/// </summary>
public class ImagePathToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasImage = false;
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            // Bỏ qua link internet
            if (!s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                hasImage = true; // Xem như hợp lệ vì đã được build dạng Resource
            }
        }
        // If parameter == "inverse", invert the result (used for the placeholder)
        var invert = parameter is string p && p == "inverse";
        var visible = hasImage ^ invert;
        return visible
            ? System.Windows.Visibility.Visible
            : System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StockToBadgeTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int qty) return "0 Out of Stock";
        if (qty == 0) return "0 Out of Stock";
        if (qty <= 15) return $"{qty} Low Stock";
        return $"{qty} In Stock";
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StockToBadgeBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int qty || qty == 0) return "#FEF2F2";
        if (qty <= 15) return "#FFFBEB";
        return "#ECFDF5";
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StockToBadgeForegroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int qty || qty == 0) return "#EF4444";
        if (qty <= 15) return "#D97706";
        return "#10B981";
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
