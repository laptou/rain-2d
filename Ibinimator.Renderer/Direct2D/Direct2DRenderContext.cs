using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace Ibinimator.Renderer.Direct2D
{
    public class Direct2DRenderContext : RenderContext
    {
        private readonly RenderTarget _target;

        public Direct2DRenderContext(RenderTarget target)
        {
            _target = target;
        }

        public override ISolidColorBrush CreateBrush(Color color)
        {
            return new SolidColorBrush(_target, color);
        }

        public override ILinearGradientBrush CreateBrush(IEnumerable<GradientStop> stops, float startX, float startY,
            float endX, float endY)
        {
            return new LinearGradientBrush(_target, stops, new RawVector2(startX, startY), new RawVector2(endX, endY));
        }

        public override IRadialGradientBrush CreateBrush(IEnumerable<GradientStop> stops, float centerX, float centerY,
            float radiusX, float radiusY,
            float focusX, float focusY)
        {
            return new RadialGradientBrush(_target, stops, new RawVector2(centerX, centerY),
                new RawVector2(radiusX, radiusY), new RawVector2(focusX, focusY));
        }

        public override IPen CreatePen(float width, IBrush brush, IEnumerable<float> dashes)
        {
            return new Pen(width, brush as Brush, dashes, _target);
        }

        public override void Dispose()
        {
            _target.Dispose();
        }

        protected override void Apply(RenderCommand command)
        {
            if (command is GeometricRenderCommand geometricCommand)
            {
                var brush = geometricCommand.Brush as Brush;
                var pen = geometricCommand.Pen as Pen;

                if (brush == null && pen == null) return;

                switch (geometricCommand)
                {
                    case EllipseRenderCommand ellipse:
                        if (ellipse.Fill)
                            _target.FillEllipse(
                                new Ellipse(
                                    new RawVector2(
                                        ellipse.CenterX,
                                        ellipse.CenterY),
                                    ellipse.RadiusX, ellipse.RadiusY),
                                brush);
                        else
                            _target.DrawEllipse(
                                new Ellipse(
                                    new RawVector2(
                                        ellipse.CenterX,
                                        ellipse.CenterY),
                                    ellipse.RadiusX, ellipse.RadiusY),
                                pen.Brush, pen.Width, pen.Style);
                        break;
                    case GeometryRenderCommand geometry:
                        if (geometry.Fill)
                            _target.FillGeometry(
                                geometry.Geometry as SharpDX.Direct2D1.Geometry,
                                brush);
                        else
                            _target.DrawGeometry(
                                geometry.Geometry as SharpDX.Direct2D1.Geometry,
                                pen.Brush, pen.Width, pen.Style);
                        break;
                    case RectangleRenderCommand rect:
                        if (rect.Fill)
                            _target.FillRectangle(
                                new RawRectangleF(
                                    rect.Left,
                                    rect.Top,
                                    rect.Right,
                                    rect.Bottom),
                                brush);
                        else
                            _target.DrawRectangle(
                                new RawRectangleF(
                                    rect.Left,
                                    rect.Top,
                                    rect.Right,
                                    rect.Bottom),
                                pen.Brush, pen.Width, pen.Style);
                        break;
                }
            }
        }

        protected override void Begin()
        {
            _target.BeginDraw();
        }

        protected override void End()
        {
            _target.EndDraw();
        }
    }
}