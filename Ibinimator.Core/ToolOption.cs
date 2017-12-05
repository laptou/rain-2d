using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Model;

namespace Ibinimator.Core {
    public class ToolOption : Model.Model
    {
        public ToolOption(string id) { Id = id; }

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

        public string Name
        {
            get => Get<string>();
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

        public IEnumerable<object> Values
        {
            get => Get<IEnumerable<object>>();
            set => Set(value);
        }
    }
}