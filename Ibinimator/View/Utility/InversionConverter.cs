using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Ibinimator.View.Utility
{
    public class InversionConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(
            object      value,
            Type        targetType,
            object      parameter,
            CultureInfo culture)
        {
            if (value is bool b)
                return !b;

            return value;
        }

        public object ConvertBack(
            object      value,
            Type        targetType,
            object      parameter,
            CultureInfo culture)
        {
            if (value is bool b)
                return !b;

            return value;
        }

        #endregion
    }
}