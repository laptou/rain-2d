using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Ibinimator.View.Utility
{
    public class EnumTemplateSelector : DataTemplateSelector
    {
        public string PropertyName { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var itemsControl = container.FindVisualAncestor<ItemsControl>();

            if (item != null &&
                itemsControl != null)
            {
                var type = item.GetType();

                var prop = type.GetProperty(PropertyName);

                if (prop != null)
                {
                    var key = prop.GetValue(item);

                    var resource = itemsControl.FindResource(key);

                    if (resource is DataTemplate template) return template;
                }
            }

            return base.SelectTemplate(item, container);
        }
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