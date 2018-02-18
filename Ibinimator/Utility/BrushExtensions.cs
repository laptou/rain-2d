using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Paint;
using Ibinimator.Renderer.WPF;

using WPF = System.Windows.Media;

namespace Ibinimator.Utility
{
    public static class BrushExtensions
    {
        public static WPF.Brush CreateWpfBrush(this IBrushInfo brush)
        {
            return brush.CreateBrush(new WpfRenderContext()).Unwrap<WPF.Brush>();
        }
    }
}