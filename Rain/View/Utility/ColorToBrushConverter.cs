using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Rain.View.Utility
{
    public class ColorToBrushConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color c)
            {
                var brush = new SolidColorBrush(c);

                return brush;
            }

            if (value is SolidColorBrush s)
                return s;

            return null;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new InvalidOperationException();
        }

        #endregion
    }
}