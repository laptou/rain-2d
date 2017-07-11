using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Ibinimator.View
{
    public class Helper
    {
        public static Brush GetAccent(DependencyObject obj)
        {
            return (Brush)obj.GetValue(AccentProperty);
        }

        public static void SetAccent(DependencyObject obj, Color value)
        {
            obj.SetValue(AccentProperty, value);
        }

        public static readonly DependencyProperty AccentProperty =
            DependencyProperty.RegisterAttached("Accent", typeof(Brush), typeof(Helper), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0, 0, 0))));
    }

    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var fraction = System.Convert.ToDecimal(value);
            return fraction.ToString("P0", culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal.TryParse(value.ToString().Trim(culture.NumberFormat.PercentSymbol[0]), NumberStyles.Any, culture.NumberFormat, out decimal d);
            return d / 100;
        }
    }
}