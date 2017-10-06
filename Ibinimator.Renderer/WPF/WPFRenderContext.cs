using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Ibinimator.Renderer.WPF
{
    public class WpfRenderContext : RenderContext
    {
        public override ISolidColorBrush CreateBrush(Color color)
        {
            return new SolidColorBrush(color);
        }

        public override ILinearGradientBrush CreateBrush(IEnumerable<GradientStop> stops, float startX, float startY,
            float endX, float endY)
        {
            return new LinearGradientBrush(
                stops,
                new Point(startX, startY),
                new Point(endX, endY));
        }

        public override IRadialGradientBrush CreateBrush(IEnumerable<GradientStop> stops, float centerX, float centerY,
            float radiusX, float radiusY,
            float focusX, float focusY)
        {
            return new RadialGradientBrush(
                stops,
                new Point(centerX, centerY),
                new Size(radiusX, radiusX),
                new Point(focusX, focusY));
        }

        public override IPen CreatePen(float width, IBrush brush, IEnumerable<float> dashes)
        {
            return new Pen(width, brush as Brush, dashes);
        }

        public override void Dispose()
        {
        }

        protected override void Apply(RenderCommand command)
        {
            throw new NotImplementedException();
        }

        protected override void Begin()
        {
            throw new NotImplementedException();
        }

        protected override void End()
        {
            throw new NotImplementedException();
        }
    }
}