using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model;

namespace Rain.Core
{
    public class ToolOptions : INotifyCollectionChanged, IEnumerable<ToolOptionBase>
    {
        private readonly Dictionary<string, ToolOptionBase> _options =
            new Dictionary<string, ToolOptionBase>();

        public event PropertyChangedEventHandler OptionChanged;

        public ToolOption<T> Create<T>(string id, ToolOptionType type, string label = null)
        {
            var op = new ToolOption<T>(id, type) {Name = label};
            op.PropertyChanged += OnOptionChanged;
            _options[id] = op;

            return op;
        }

        public T Get<T>(string id)
        {
            if (!(_options[id] is ToolOption<T> option))
                return default;

            switch (option.Value)
            {
                case T t:

                    return t;
                default:

                    return (T) Convert.ChangeType(option.Value, typeof(T));
            }
        }

        public ToolOption<T> GetOption<T>(string id) { return (ToolOption<T>) _options[id]; }

        public void Set<T>(string id, T value)
        {
            if (_options[id] is ToolOption<T> option)
                option.Value = value;
        }

        private void OnOptionChanged(object sender, PropertyChangedEventArgs e)
        {
            OptionChanged?.Invoke(sender, e);
        }

        #region IEnumerable<ToolOptionBase> Members

        public IEnumerator<ToolOptionBase> GetEnumerator()
        {
            return _options.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion
    }
}