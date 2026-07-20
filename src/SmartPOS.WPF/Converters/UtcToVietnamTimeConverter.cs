using System;
using System.Globalization;
using System.Windows.Data;

namespace SmartPOS.WPF.Converters;

public class UtcToVietnamTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            var utcTime = dateTime.Kind == DateTimeKind.Unspecified 
                ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc) 
                : dateTime.ToUniversalTime();

            try
            {
                var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(utcTime, vnTimeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                return utcTime.AddHours(7);
            }
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
