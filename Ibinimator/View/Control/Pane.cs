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
        public Pane() { Loaded += OnLoaded; }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Application.LoadComponent(this, Source);
        }

        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            "Source", typeof(Uri), typeof(Pane), new PropertyMetadata(SourceChanged));

        [Category("Common")]
        public Uri Source
        {
            get => (Uri) GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Pane pane && pane.Source != null && pane.IsLoaded)
            {
                Application.LoadComponent(pane, pane.Source);

                //if (content is Pane sourcePane)
                //    pane.Content = sourcePane.Content;
                //else
                //    pane.Content = content;
            }
        }
    }
}