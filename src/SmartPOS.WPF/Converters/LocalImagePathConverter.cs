using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SmartPOS.WPF.Converters;

/// <summary>
/// Converts a local file path (ImagePath) to a BitmapImage for display in WPF Image controls.
/// Supports:
///   - Absolute disk paths returned by OpenFileDialog (e.g. C:\Users\...\photo.jpg)
///   - Embedded resource paths starting with "Assets/" (converted to Pack URI)
/// Falls back to null (shows placeholder) if path is invalid or file not found.
/// </summary>
public class LocalImagePathConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            // Block internet URLs — not supported in this offline POS
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return null;

            Uri uri;

            if (path.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
            {
                // Already a WPF Pack URI (e.g. embedded resource)
                uri = new Uri(path, UriKind.Absolute);
            }
            else if (Path.IsPathRooted(path))
            {
                // Absolute local file path from OpenFileDialog (e.g. C:\Users\photo.jpg)
                if (!File.Exists(path))
                    return null;
                uri = new Uri(path, UriKind.Absolute);
            }
            else
            {
                // Relative path — treat as embedded Assets resource
                string normalized = path.Replace('\\', '/');
                string packPath = normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                    ? normalized
                    : $"Assets/Images/{normalized}";
                uri = new Uri($"pack://application:,,,/SmartPOS.WPF;component/{packPath}", UriKind.Absolute);
            }

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = uri;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 300;
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
/// Converts ImagePath string: returns Visibility.Visible if path is valid and exists, Collapsed otherwise.
/// Used to show/hide the image preview and the placeholder icon.
/// </summary>
public class ImagePathToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasImage = false;
        if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            // Block internet URLs
            if (!s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (Path.IsPathRooted(s))
                    // Absolute disk path — only valid if the file actually exists
                    hasImage = File.Exists(s);
                else
                    // Relative / Pack URI — assume valid (resource embedded in app)
                    hasImage = true;
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
