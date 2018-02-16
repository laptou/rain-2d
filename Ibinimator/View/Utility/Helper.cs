using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using FPMO = System.Windows.FrameworkPropertyMetadataOptions;

namespace Ibinimator.View.Utility
{
    public class Helper
    {
        public static readonly DependencyProperty AccentProperty =
            DependencyProperty.RegisterAttached("Accent",
                                                typeof(Color),
                                                typeof(Helper),
                                                new FrameworkPropertyMetadata(
                                                    Color.FromRgb(255, 255, 255),
                                                    FPMO.Inherits | FPMO.AffectsRender));

        public static readonly DependencyProperty InputBindingSourceProperty =
            DependencyProperty.RegisterAttached("InputBindingSource",
                                                typeof(IEnumerable<InputBinding>),
                                                typeof(Helper),
                                                new PropertyMetadata(InputBindingsChanged));

        public static readonly DependencyProperty ElevationProperty =
            DependencyProperty.RegisterAttached("Elevation",
                                                typeof(int),
                                                typeof(Helper),
                                                new FrameworkPropertyMetadata(
                                                    0,
                                                    FPMO.AffectsRender));

        public static Color GetAccent(DependencyObject obj)
        {
            return (Color) obj.GetValue(AccentProperty);
        }

        public static int GetElevation(DependencyObject element)
        {
            return (int) element.GetValue(ElevationProperty);
        }

        public static IEnumerable<InputBinding> GetInputBindingSource(DependencyObject element)
        {
            return (IEnumerable<InputBinding>) element.GetValue(InputBindingSourceProperty);
        }

        public static void SetAccent(DependencyObject obj, Color value)
        {
            obj.SetValue(AccentProperty, value);
        }

        public static void SetElevation(DependencyObject element, int value)
        {
            element.SetValue(ElevationProperty, value);
        }

        public static void SetInputBindingSource(
            DependencyObject element, IEnumerable<InputBinding> value)
        {
            element.SetValue(InputBindingSourceProperty, value);
        }

        private static void InputBindingsChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var value = (IEnumerable<InputBinding>) e.NewValue;

            if (d is UIElement ui)
            {
                ui.InputBindings.Clear();

                foreach (var inputBinding in value)
                    ui.InputBindings.Add(inputBinding);
            }
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

        public object ConvertBack(
            object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal.TryParse(value.ToString().Trim(culture.NumberFormat.PercentSymbol[0]),
                             NumberStyles.Any,
                             culture.NumberFormat,
                             out var d);

            return d / 100;
        }

        #endregion
    }
}