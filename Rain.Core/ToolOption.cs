using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Measurement;

namespace Rain.Core
{
    public abstract class ToolOptionBase : Model.Model
    {
        protected ToolOptionBase(string id, ToolOptionType type)
        {
            Id = id;
            Type = type;
        }

        public string Icon
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Id { get; }

        public float Maximum
        {
            get => Get<float>();
            set => Set(value);
        }

        public float Minimum
        {
            get => Get<float>();
            set => Set(value);
        }

        public string Label
        {
            get => Get<string>();
            set => Set(value);
        }

        public ToolOptionType Type
        {
            get => Get<ToolOptionType>();
            private set => Set(value);
        }

        public Unit Unit
        {
            get => Get<Unit>();
            set => Set(value);
        }
    }

    public class ToolOption<T> : ToolOptionBase
    {
        /// <inheritdoc />
        public ToolOption(string id, ToolOptionType type) : base(id, type) { }

        public T Value
        {
            get => Get<T>();
            set => base.Set(value);
        }

        public IEnumerable<T> Values
        {
            get => Get<IEnumerable<T>>();
            set => Set(value);
        }

        #region Convenience Setters

        // These methods allow calls to be chained together

        public ToolOption<T> Set(T value)
        {
            Value = value;

            return this;
        }

        public ToolOption<T> SetIcon(string icon)
        {
            Icon = icon;

            return this;
        }

        public ToolOption<T> SetMaximum(float maximum)
        {
            Maximum = maximum;

            return this;
        }

        public ToolOption<T> SetMinimum(float minimum)
        {
            Minimum = minimum;

            return this;
        }

        public ToolOption<T> SetUnit(Unit unit)
        {
            Unit = unit;

            return this;
        }

        public ToolOption<T> SetValues(IEnumerable<T> values)
        {
            Values = values;

            return this;
        }

        public ToolOption<T> SetValues(params T[] values) { return SetValues(values.AsEnumerable()); }

        #endregion
    }
}