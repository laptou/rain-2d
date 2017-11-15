using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Core.Model;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Model
{
    public class ToolOption : Core.Model.Model, IToolOption
    {
        public string Icon
        {
            get => Get<string>();
            set => Set(value);
        }

        public object Maximum
        {
            get => Get<object>();
            set => Set(value);
        }

        public object Minimum
        {
            get => Get<object>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public IEnumerable<object> Options
        {
            get => Get<object[]>();
            set => Set(value);
        }

        public ToolOptionType Type
        {
            get => Get<ToolOptionType>();
            set => Set(value);
        }

        public Unit Unit
        {
            get => Get<Unit>();
            set => Set(value);
        }

        public object Value
        {
            get => Get<object>();
            set => Set(value);
        }

        public void SetValue(object value) { SilentSet(value, nameof(Value)); }
    }

    public class ToolOption<T> : Core.Model.Model, IToolOption
    {
        public ToolOption(string name, ToolOptionType type)
        {
            Name = name;
            Type = type;
        }

        public string Icon { get; set; }

        object IToolOption.Maximum
        {
            get => Maximum;
            set => Maximum = Cast(value);
        }

        object IToolOption.Minimum
        {
            get => Minimum;
            set => Minimum = Cast(value);
        }

        public string Name { get; set; }

        IEnumerable<object> IToolOption.Options
        {
            get => Options.Cast<object>();
            set => Options = value.Select(Cast);
        }

        private static T Cast(object o) => (T) Convert.ChangeType(o, typeof(T));

        public T Maximum
        {
            get => Get<T>();
            set => Set(value);
        }

        public T Minimum
        {
            get => Get<T>();
            set => Set(value);
        }

        public IEnumerable<T> Options
        {
            get => Get<T[]>();
            set => Set(value);
        }

        public ToolOptionType Type
        {
            get => Get<ToolOptionType>();
            set => Set(value);
        }

        public Unit Unit { get; set; }

        object IToolOption.Value
        {
            get => Value;
            set => Value = Cast(value);
        }

        public void SetValue(object value)
        {
            SetValue(Cast(value));
        }

        public T Value
        {
            get => Get<T>();
            set => Set(Validate(value));
        }

        public void SetValue(T value) { SilentSet(value, nameof(Value)); }

        private T Validate(T value)
        {
            switch (System.Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Byte:
                case TypeCode.DateTime:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    var comparable = (IComparable<T>) value;

                    if (comparable.CompareTo(Minimum) < 0)
                        return Minimum;

                    if (comparable.CompareTo(Maximum) > 0)
                        return Maximum;

                    return value;
                default:
                    return value;
            }
        }
    }
}