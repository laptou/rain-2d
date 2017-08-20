using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Ibinimator.Model;
using Ibinimator.Shared;

namespace Ibinimator.View
{
    public class UnitConverter : DependencyObject, IValueConverter
    {
        public Unit SourceUnit
        {
            get => (Unit)GetValue(SourceUnitProperty);
            set => SetValue(SourceUnitProperty, value);
        }

        public Unit TargetUnit
        {
            get => (Unit)GetValue(TargetUnitProperty);
            set => SetValue(TargetUnitProperty, value);
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SourceUnitProperty =
            DependencyProperty.Register("SourceUnit", typeof(Unit), typeof(UnitConverter), 
                new PropertyMetadata(Unit.Radians));

        public static readonly DependencyProperty TargetUnitProperty =
            DependencyProperty.Register("TargetUnit", typeof(Unit), typeof(UnitConverter),
                new PropertyMetadata(Unit.Degrees));


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is float input)
                if (Type.GetTypeCode(targetType) == TypeCode.String)
                    return Format(input * ConversionFactor(SourceUnit, TargetUnit), TargetUnit);
                else
                    return input * ConversionFactor(SourceUnit, TargetUnit);
            else
                throw new InvalidOperationException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string input)
            {
                var num = Unformat(input, TargetUnit) * ConversionFactor(TargetUnit, SourceUnit);
                if (!float.IsNaN(num)) return num;
            }
            else
            {
                return System.Convert.ToSingle(value) * ConversionFactor(TargetUnit, SourceUnit);
            }
            return Binding.DoNothing;
        }

        public float ConversionFactor(Unit source, Unit target)
        {
            Dictionary<Unit, float> factor = new Dictionary<Unit, float>
            {
                [Unit.Radians] = 1f,
                [Unit.Degrees] = 360f / MathUtils.TwoPi,
                [Unit.Pixels] = 1f,
                [Unit.Inches] = 96f,
                [Unit.Centimeters] = 96f / 2.54f
            };

            return factor[target] / factor[source];
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
                case Unit.Inches:
                    return $"{value:N1} in";
                case Unit.Centimeters:
                    return $"{value:N1} cm";
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }
        }

        public float Unformat(string value, Unit target)
        {
            var parts = value.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            
            if (!float.TryParse(parts[0], out float num)) return float.NaN;

            Unit source;

            switch (parts.ElementAtOrDefault(1))
            {
                case null:
                    source = target;
                    break;

                case "\u00B0":
                    source = Unit.Degrees;
                    break;

                case "px":
                    source = Unit.Pixels;
                    break;

                case "in":
                    source = Unit.Inches;
                    break;

                case "cm":
                    source = Unit.Centimeters;
                    break;

                default:
                    return float.NaN;
            }

            return num * ConversionFactor(source, target);
        }
    }

}
