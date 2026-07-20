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
            bool isUrl = path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                         path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

            if (!isUrl && !File.Exists(path))
                return null;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
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
        var hasImage = value is string s && !string.IsNullOrWhiteSpace(s) && 
                       (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                        s.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || 
                        File.Exists(s));
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
