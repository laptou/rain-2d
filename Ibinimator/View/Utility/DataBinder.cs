using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Ibinimator.View.Util
{
    public static class DataBinder
    {
        private static readonly DependencyProperty DummyProperty = DependencyProperty.RegisterAttached(
            "Dummy",
            typeof(object),
            typeof(DependencyObject),
            new UIPropertyMetadata(null));

        public static object Eval(object container, string expression)
        {
            var binding = new Binding(expression) {Source = container};
            return binding.Eval();
        }

        public static object Eval(this Binding binding, DependencyObject dependencyObject = null)
        {
            dependencyObject = dependencyObject ?? new DependencyObject();
            BindingOperations.SetBinding(dependencyObject, DummyProperty, binding);
            return dependencyObject.GetValue(DummyProperty);
        }
    }
}