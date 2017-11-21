using System;
using System.Globalization;
using System.Windows.Data;

namespace Ibinimator.View.Utility
{
    public class InversionConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value is bool b)
                return !b;

            return value;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value is bool b)
                return !b;

            return value;
        }
    }
}