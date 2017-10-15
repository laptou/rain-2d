using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Color = Ibinimator.Core.Color;

namespace Ibinimator.Renderer.WPF
{
    public class WpfRenderContext : RenderContext
    {
        private DrawingContext _ctx;

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

        public override ITextLayout CreateTextLayout()
        {
            throw new NotImplementedException();
        }

        public override IGeometry CreateGeometry()
        {
            return new Geometry();
        }

        public override IGeometry CreateEllipseGeometry(float cx, float cy, float rx, float ry)
        {
            return new Geometry(new EllipseGeometry(new Point(cx, cy), rx, ry));
        }

        public override IGeometry CreateRectangleGeometry(float x, float y, float w, float h)
        {
            return new Geometry(new RectangleGeometry(new Rect(new Point(x, y), new Size(w, h))));
        }

        public override IGeometry CreateGeometryGroup(params IGeometry[] geometries)
        {
            return new Geometry(new GeometryGroup
            {
                Children = new GeometryCollection(
                    geometries.Select(g => (System.Windows.Media.Geometry)g))
            });
        }

        public override void Dispose()
        {
            _ctx = null;
        }

        protected override void Apply(RenderCommand command)
        {
            switch (command)
            {
                case EllipseRenderCommand ellipse:
                    _ctx?.DrawEllipse(
                        ellipse.Brush as Brush, 
                        ellipse.Pen as Pen,
                        new Point(ellipse.CenterX, ellipse.CenterY),
                        ellipse.RadiusX, ellipse.RadiusY);
                    break;
                case GeometryRenderCommand geometry:
                    _ctx?.DrawGeometry(
                        geometry.Brush as Brush,
                        geometry.Pen as Pen,
                        geometry.Geometry as Geometry);
                    break;
                case RectangleRenderCommand rect:
                    _ctx?.DrawRectangle(
                        rect.Brush as Brush,
                        rect.Pen as Pen,
                        new Rect(rect.Left, rect.Top, rect.Width, rect.Height));
                    break;
                case TransformRenderCommand transform:
                    _ctx?.PushTransform(
                        new MatrixTransform(
                            transform.Transform.M11,
                            transform.Transform.M12,
                            transform.Transform.M21,
                            transform.Transform.M22,
                            transform.Transform.M31,
                            transform.Transform.M32));
                    break;
            }
        }

        protected override void Begin(object ctx)
        {
            if (ctx is DrawingContext dc)
                _ctx = dc;
        }

        protected override void End()
        {
            _ctx = null;
        }
    }
}