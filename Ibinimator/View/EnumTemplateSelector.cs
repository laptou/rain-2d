using System.Windows;
using System.Windows.Controls;

namespace Ibinimator.View
{
    public class EnumTemplateSelector : DataTemplateSelector
    {
        public string PropertyName { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var itemsControl = container.FindVisualAncestor<ItemsControl>();

            if (item != null && itemsControl != null)
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
}