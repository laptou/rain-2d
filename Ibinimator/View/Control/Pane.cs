using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Ibinimator.View.Control
{
    public class Pane : TabItem
    {
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            "Source", typeof(Uri), typeof(Pane), new PropertyMetadata(SourceChanged));

        public Pane() { Loaded += OnLoaded; }

        [Category("Common")]
        public Uri Source
        {
            get => (Uri) GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Application.LoadComponent(this, Source);
        }

        private static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Pane pane && pane.Source != null && pane.IsLoaded)
                Application.LoadComponent(pane, pane.Source);
        }
    }
}