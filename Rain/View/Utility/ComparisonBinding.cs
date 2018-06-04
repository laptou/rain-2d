using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Rain.View.Utility
{
    // Supported types of comparisons
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum ComparisonOperators
    {
        EQ = 0,
        NE,
        GT,
        GTE,
        LT,
        LTE
    }

    // ComparisonBinding is a Binding that should be used in a
    // DataTrigger.Binding. It supports a comparison operator and a
    // comparand, so that you can use it as a conditional DataTrigger.
    // The trick is to set {x:Null} as the DataTrigger.Value. E.g.:
    //
    // <DataTrigger Value={x:Null} Binding={h:ComparisonBinding Width,
    // EQ, 100}"
    //
    // The operator can be EQ, LT, LTE, GT, GTE.
    public class ComparisonBinding : Binding
    {
        // Default constructor
        public ComparisonBinding() : this(null, ComparisonOperators.EQ, null) { }

        // Construction with an operator & comparand
        public ComparisonBinding(string path, ComparisonOperators op, object comparand) : base(path)
        {
            Comparand = comparand;
            Operator = op;
            Converter = new ComparisonConverter(this);
        }

        public object Comparand { get; set; }

        // Operator and comparand
        public ComparisonOperators Operator { get; set; }
    }

    // Thie IValueConverter is used by the StyleBinding to implement
    // the logical comparisson. ConvertBack isn’t supported. Convert
    // returns null if the condition is met, non-null otherwise.
    internal class ComparisonConverter : IValueConverter
    {
        // Return this if the condition isn’t met
        private static readonly object _notNull = new object();

        // Keep a back reference to the StyleBinding
        private readonly ComparisonBinding _styleBinding;

        // In construction, get a reference to the StyleBinding
        public ComparisonConverter(ComparisonBinding styleBinding) { _styleBinding = styleBinding; }

        // Check for equality of two values
        private bool CheckEquals(Type type, object value1, object value2)
        {
            if (type.IsValueType ||
                type == typeof(string))
                return Equals(value1, value2);

            return ReferenceEquals(value1, value2);
        }

        // This helper produces the return value; null if the values
        // match, non-null otherwise.
        private object ReturnHelper(bool result)
        {
            return result; //? null : _notNull;
        }

        // Trace output to the debugger
        private void Trace(object value, string message)
        {
            if (Debugger.IsAttached)
            {
                Debug.WriteLine("StyleBinding couldn’t convert '" + value.GetType() + "' to '" +
                                _styleBinding.Comparand.GetType() + "'");
                Debug.WriteLine("(" + message + ")");
            }
        }

        #region IValueConverter Members

        // IValueConverter.Convert
        //
        // Return null of the condition is met, non-null if not.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Simple check for null
            if (value == null ||
                _styleBinding.Comparand == null)
                if (_styleBinding.Operator == ComparisonOperators.NE)
                    return ReturnHelper(value != _styleBinding.Comparand);
                else
                    return ReturnHelper(value == _styleBinding.Comparand);

            // Convert the comparand so that it matches the value
            var convertedComparand = _styleBinding.Comparand;

            if (_styleBinding.Comparand is Binding b)
                convertedComparand = b.Eval();

            try
            {
                // Only support simple conversions in here.
                convertedComparand = System.Convert.ChangeType(_styleBinding.Comparand, value.GetType());
            }
            catch (InvalidCastException)
            {
                // If Convert.ChangeType didn’t work, try a type converter
                var typeConverter = TypeDescriptor.GetConverter(value);
                if (typeConverter?.CanConvertFrom(_styleBinding.Comparand.GetType()) == true)
                    convertedComparand = typeConverter.ConvertFrom(_styleBinding.Comparand);
            }

            // Simple check for the equality case
            if (_styleBinding.Operator == ComparisonOperators.EQ)
                return ReturnHelper(CheckEquals(value.GetType(), value, convertedComparand));

            if (_styleBinding.Operator == ComparisonOperators.NE)
                return ReturnHelper(!CheckEquals(value.GetType(), value, convertedComparand));

            // For anything other than Equals, we need IComparable
            if (!(value is IComparable) ||
                !(convertedComparand is IComparable))
            {
                Trace(value, "One of the values was not an IComparable");

                return ReturnHelper(false);
            }

            // Compare the values
            var comparison = (value as IComparable).CompareTo(convertedComparand);

            // And return the comparisson result
            switch (_styleBinding.Operator)
            {
                case ComparisonOperators.GT:

                    return ReturnHelper(comparison > 0);

                case ComparisonOperators.GTE:

                    return ReturnHelper(comparison >= 0);

                case ComparisonOperators.LT:

                    return ReturnHelper(comparison < 0);

                case ComparisonOperators.LTE:

                    return ReturnHelper(comparison <= 0);
            }

            return _notNull;
        }

        // IValueConverter.ConvertBack isn’t supported.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}