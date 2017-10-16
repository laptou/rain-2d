using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer.Model;
using Ibinimator.Utility;

namespace Ibinimator.View.Util
{
    public class UnitConverter : DependencyObject, IValueConverter, IMultiValueConverter
    {
        private static readonly Dictionary<Unit, float> Factors = new Dictionary<Unit, float>
        {
            [Unit.Radians] = 1f,
            [Unit.Degrees] = 360f / MathUtils.TwoPi,
            [Unit.Pixels] = 1f,
            [Unit.Points] = 96f / 72f,
            [Unit.Inches] = 96f,
            [Unit.Centimeters] = 96f / 2.54f,
            [Unit.Millimeters] = 96f / 25.4f,
            [Unit.Milliseconds] = 0.001f,
            [Unit.Seconds] = 1,
            [Unit.Minutes] = 60,
            [Unit.Hours] = 3600
        };

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BaseUnitProperty =
            DependencyProperty.Register("BaseUnit", typeof(Unit), typeof(UnitConverter),
                new PropertyMetadata(Unit.Radians));

        public Unit BaseUnit
        {
            get => (Unit) GetValue(BaseUnitProperty);
            set => SetValue(BaseUnitProperty, value);
        }

        public static float ConversionFactor(Unit source, Unit target)
        {
            if (source == Unit.None || target == Unit.None) return 1;
            return Factors[target] / Factors[source];
        }

        public string Format(float value, Unit target)
        {
            switch (target)
            {
                case Unit.Radians:
                    return $"{value:N1}";
                case Unit.Degrees:
                    return $"{value:N1} \u00B0";
                case Unit.Pixels:
                    return $"{value:N1} px";
                case Unit.Points:
                    return $"{value:N1} pt";
                case Unit.Inches:
                    return $"{value:N1} in";
                case Unit.Centimeters:
                    return $"{value:N1} cm";
                case Unit.Millimeters:
                    return $"{value:N1} mm";
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }
        }

        public static Unit GetUnit(string suffix, UnitType type)
        {
            switch (suffix)
            {
                case "" when type == UnitType.Angle: return Unit.Radians;
                case "px":
                case "" when type == UnitType.Length: return Unit.Pixels;
                case "s":
                case "" when type == UnitType.Time: return Unit.Seconds;

                case "pt": return Unit.Points;
                case "mm": return Unit.Millimeters;
                case "cm": return Unit.Centimeters;
                case "in": return Unit.Inches;

                case "ms": return Unit.Milliseconds;
                case "f": return Unit.Frames;
                case "m": return Unit.Minutes;
                case "h": return Unit.Hours;

                case "deg":
                case "\u00B0": return Unit.Degrees;
                default: return Unit.None;
            }
        }

        public float Unformat(string value, Unit target)
        {
            var parts = Regex.Match(value,
                @"([-+]?(?:(?:[0-9]*\.[0-9]+)|(?:[0-9]+))(?:E[-+]?[0-9]+)?)\s*([a-zA-Z%\u00B0]*)");

            if (!float.TryParse(parts.Groups[1].Value, out var num)) return float.NaN;

            Unit source;

            switch (parts.Groups[2].Value)
            {
                case "\u00B0":
                    source = Unit.Degrees;
                    break;

                case "px":
                    source = Unit.Pixels;
                    break;

                case "pt":
                    source = Unit.Points;
                    break;

                case "in":
                    source = Unit.Inches;
                    break;

                case "cm":
                    source = Unit.Centimeters;
                    break;

                default:
                    source = Unit.None;
                    break;
            }

            return num / ConversionFactor(source, target);
        }

        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(values[0], targetType, values.ElementAtOrDefault(1) ?? parameter, culture);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new[]
            {
                ConvertBack(value, targetTypes[0], parameter, culture),
                BaseUnit
            };
        }

        #endregion

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var unit = parameter as Unit? ?? BaseUnit;

            if (value == null) return 0;

            if (value.GetType().IsNumeric() || value is string)
            {
                var input = System.Convert.ToSingle(value);

                input *= ConversionFactor(BaseUnit, unit);

                if (Type.GetTypeCode(targetType) == TypeCode.String)
                    return Format(input, unit);

                return input;
            }

            throw new ArgumentException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var unit = parameter as Unit? ?? BaseUnit;

            if (value is string input)
            {
                var num = Unformat(input, unit) * ConversionFactor(unit, BaseUnit);
                if (!float.IsNaN(num)) return num;
            }
            else
            {
                return System.Convert.ToSingle(value) * ConversionFactor(unit, BaseUnit);
            }

            return Binding.DoNothing;
        }

        #endregion
    }
}