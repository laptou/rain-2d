using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Ibinimator.View.Control
{
    public class VectorIcon : ContentControl
    {
        public VectorIcon()
        {
            Loaded += (s, e) =>
            {
                var path = IconPath.ToString().Split('#').First();
                var hash = IconPath.ToString().Split('#').Last();

                var info = App.LoadComponent(new Uri(path, UriKind.Relative));
                var g = info as ResourceDictionary;
                var c = (g[hash] as Canvas);

                c.Width = 100;
                c.Height = 100;

                Content = c;
            };
        }

        public Uri IconPath
        {
            get { return (Uri)GetValue(IconPathProperty); }
            set { SetValue(IconPathProperty, value); }
        }
        
        public static readonly DependencyProperty IconPathProperty =
            DependencyProperty.Register("IconPath", typeof(Uri), typeof(VectorIcon), new PropertyMetadata(null));
    }
}
