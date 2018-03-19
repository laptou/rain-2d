using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

using Rain.ViewModel;

namespace Rain.View.Utility
{
    public class EnumDataTemplateSelector : DataTemplateSelector, IDictionary
    {
        private readonly IDictionary _dict = new Dictionary<Enum, DataTemplate>();

        public string PropertyName { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var itemsControl = container.FindVisualAncestor<ItemsControl>();

            if (item != null)
            {
                var type = item.GetType();

                var prop = type.GetProperty(PropertyName);

                if (prop != null)
                {
                    var key = prop.GetValue(item);

                    if (key is Enum e)
                    {
                        if (Contains(e))
                            return this[e] as DataTemplate;

                        var resource = itemsControl?.FindResource(key);

                        if (resource is DataTemplate template)
                            return template;
                    }
                }
            }

            return base.SelectTemplate(item, container);
        }

        /// <inheritdoc />
        public bool Contains(object key) { return _dict.Contains(key); }

        /// <inheritdoc />
        public void Add(object key, object value) { _dict.Add(key, value); }

        /// <inheritdoc />
        public void Clear() { _dict.Clear(); }

        /// <inheritdoc />
        public IDictionaryEnumerator GetEnumerator() { return _dict.GetEnumerator(); }

        /// <inheritdoc />
        public void Remove(object key) { _dict.Remove(key); }

        /// <inheritdoc />
        public object this[object key]
        {
            get => _dict[key];
            set => _dict[key] = value;
        }

        /// <inheritdoc />
        public ICollection Keys => _dict.Keys;

        /// <inheritdoc />
        public ICollection Values => _dict.Values;

        /// <inheritdoc />
        public bool IsReadOnly => _dict.IsReadOnly;

        /// <inheritdoc />
        public bool IsFixedSize => _dict.IsFixedSize;

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() { return ((IEnumerable) _dict).GetEnumerator(); }

        /// <inheritdoc />
        public void CopyTo(Array array, int index) { _dict.CopyTo(array, index); }

        /// <inheritdoc />
        public int Count => _dict.Count;

        /// <inheritdoc />
        public object SyncRoot => _dict.SyncRoot;

        /// <inheritdoc />
        public bool IsSynchronized => _dict.IsSynchronized;
    }

    public class EnumItemContainerTemplateSelector : ItemContainerTemplateSelector
    {
        public string PropertyName { get; set; }

        public override DataTemplate SelectTemplate(object item, ItemsControl itemsControl)
        {
            if (item != null &&
                itemsControl != null)
            {
                var type = item.GetType();

                var prop = type.GetProperty(PropertyName);

                if (prop != null)
                {
                    var key = prop.GetValue(item);

                    var resource = itemsControl.TryFindResource(key);

                    if (resource is DataTemplate template) return template;
                }
            }

            return base.SelectTemplate(item, itemsControl);
        }
    }
}