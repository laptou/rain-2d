using System;
using System.Globalization;
using System.Windows.Data;

namespace Ibinimator.View
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Equals(value, parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            return Equals(value, true) ? parameter : Binding.DoNothing;
        }
    }
}