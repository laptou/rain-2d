using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media;
using Ibinimator.View.Control;
using WPF = System.Windows;

namespace Ibinimator.View.Util
{
    public class SvgExtension : MarkupExtension
    {
        public SvgExtension(Uri path)
        {
            Path = path;
        }

        public Uri Path { get; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var viewbox = new VisualBrush {Visual = new SvgImage {Source = Path}};


            return viewbox;
        }
    }
}