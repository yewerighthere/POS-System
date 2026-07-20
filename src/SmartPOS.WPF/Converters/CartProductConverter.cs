using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using SmartPOS.Shared.DTOs.Cart;

namespace SmartPOS.WPF.Converters
{
    public class CartProductQuantityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] is CartSummaryDto
            // values[1] is Guid (ProductId)
            if (values.Length >= 2 && values[0] is CartSummaryDto cart && values[1] is Guid productId)
            {
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                return item != null ? $"x{item.Quantity}" : "0";
            }
            return "0";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class CartProductInCartVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameterVal, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is CartSummaryDto cart && values[1] is Guid productId)
            {
                var inCart = cart.Items.Any(i => i.ProductId == productId);
                // Parameter "inverse" to invert visibility
                var invert = false;
                if (parameterVal is string p1 && p1 == "inverse")
                {
                    invert = true;
                }
                var visible = inCart ^ invert;
                return visible ? Visibility.Visible : Visibility.Collapsed;
            }
            if (parameterVal is string p2 && p2 == "inverse")
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameterVal, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
