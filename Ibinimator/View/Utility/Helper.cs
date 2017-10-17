using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Ibinimator.View.Util
{
    public class Helper
    {
        public static readonly DependencyProperty AccentProperty =
            DependencyProperty.RegisterAttached("Accent", typeof(Color), typeof(Helper),
                new FrameworkPropertyMetadata(
                    Color.FromRgb(0, 0, 0),
                    FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyPropertyKey AccentBrushPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("AccentBrush", typeof(SolidColorBrush),
                typeof(Helper), new PropertyMetadata(default(SolidColorBrush)));

        public static Color GetAccent(DependencyObject obj)
        {
            return (Color) obj.GetValue(AccentProperty);
        }

        public static SolidColorBrush GetAccentBrush(DependencyObject obj)
        {
            return new SolidColorBrush(GetAccent(obj));
        }

        public static void SetAccent(DependencyObject obj, Color value)
        {
            obj.SetValue(AccentProperty, value);
        }

        public static void SetAccentBrush(DependencyObject obj, SolidColorBrush value)
        {
        }
    }

    public class PercentageConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var fraction = System.Convert.ToDecimal(value);
            return fraction.ToString("P0", culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal.TryParse(value.ToString().Trim(culture.NumberFormat.PercentSymbol[0]), NumberStyles.Any,
                culture.NumberFormat, out var d);
            return d / 100;
        }

        #endregion
    }
}