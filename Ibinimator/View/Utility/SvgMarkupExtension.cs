using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Ibinimator.Svg;
using Ibinimator.View.Control;
using WPF = System.Windows;

namespace Ibinimator.View.Util
{
    public class SvgExtension : MarkupExtension
    {
        public Uri Path { get; }

        public SvgExtension(Uri path)
        {
            Path = path;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var viewbox = new VisualBrush {Visual = new SvgImage {Source = Path }};


            return viewbox;
        }
    }
}
