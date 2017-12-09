using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Model;

namespace Ibinimator.Core {
    public class ToolOptions : INotifyCollectionChanged, IEnumerable<ToolOption>
    {
        private readonly Dictionary<string, ToolOption> _options = new Dictionary<string, ToolOption>();
        private bool _reentrancyFlag;

        public void Create(string id, string name)
        {
            var op = new ToolOption(id) {Name = name};
            op.PropertyChanged += OnOptionChanged;
            _options[id] = op;
        }

        private void OnOptionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_reentrancyFlag) return;

            _reentrancyFlag = true;
            OptionChanged?.Invoke(sender, e);
            _reentrancyFlag = false;
        }

        public T Get<T>(string id)
        {
            switch (_options[id].Value) {
                case T t:
                    return t;
                case null:
                    return default;
                default:
                    return (T) Convert.ChangeType(_options[id].Value, typeof(T));
            }
        }
        
        public void SetMaximum(string id, float maximum) { _options[id].Maximum = maximum; }
        public void SetMinimum(string id, float minimum) { _options[id].Minimum = minimum; }
        public void SetType(string id, ToolOptionType type) { _options[id].Type = type; }
        public void SetValues<T>(string id, IEnumerable<T> values)
        {
            _reentrancyFlag = true;
            _options[id].Values = values.Cast<object>();
            _reentrancyFlag = false;
        }

        #region IEnumerable<ToolOption> Members

        public IEnumerator<ToolOption> GetEnumerator() { return _options.Values.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        public event PropertyChangedEventHandler OptionChanged;

        public void SetUnit(string id, Unit unit) { _options[id].Unit = unit; }

        public void Set<T>(string id, T value)
        {
            _reentrancyFlag = true;
            _options[id].Value = value;
            _reentrancyFlag = false;
        }
    }
}