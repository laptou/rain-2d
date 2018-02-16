using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace Ibinimator.View.Utility
{
    public class MultiplyExtension : Binding
    {
        public MultiplyExtension(object operand1, object operand2)
        {
            Operand1 = operand1;
            Operand2 = operand2;
        }

        public object Operand1 { get; }
        public object Operand2 { get; }

        private decimal Resolve(object o, IServiceProvider serviceProvider)
        {
            if (o is MarkupExtension ext)
                o = Resolve(ext.ProvideValue(serviceProvider), serviceProvider);

            if (o is double ||
                o is float ||
                o is int ||
                o is decimal ||
                o is uint ||
                o is long ||
                o is ulong)
                return Convert.ToDecimal(Operand1);

            throw new InvalidCastException("The object could not be converted to a decimal.");
        }
    }
}