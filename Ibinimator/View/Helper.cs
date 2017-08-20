using Ibinimator.Shared;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Ibinimator.View
{
    public class Helper
    {
        public static Brush GetAccentBrush(DependencyObject obj)
        {
            return new SolidColorBrush(GetAccent(obj));
        }

        public static Color GetAccent(DependencyObject obj)
        {
            return (Color)obj.GetValue(AccentProperty);
        }

        public static void SetAccent(DependencyObject obj, Color value)
        {
            obj.SetValue(AccentProperty, value);
        }

        public static readonly DependencyProperty AccentProperty =
            DependencyProperty.RegisterAttached("Accent", typeof(Color), typeof(Helper), 
                new FrameworkPropertyMetadata(
                    ColorUtils.RgbaToColor(0, 0, 0, 1),
                    FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));
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