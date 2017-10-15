using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;
using D2D = SharpDX.Direct2D1;
using DW = SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;

namespace Ibinimator.Renderer.Direct2D
{
    public class Direct2DRenderContext : RenderContext
    {
        public Direct2DRenderContext(D2D.RenderTarget target)
        {
            Target = target;
            FactoryDW = new DW.Factory(DW.FactoryType.Shared);
        }

        public D2D.RenderTarget Target { get; }

        public D2D.Factory Factory2D => Target.Factory;

        public DW.Factory FactoryDW { get; }

        public override ISolidColorBrush CreateBrush(Color color)
        {
            return new SolidColorBrush(Target, color);
        }

        public override ILinearGradientBrush CreateBrush(IEnumerable<GradientStop> stops, float startX, float startY,
            float endX, float endY)
        {
            return new LinearGradientBrush(Target, stops, new RawVector2(startX, startY), new RawVector2(endX, endY));
        }

        public override IRadialGradientBrush CreateBrush(IEnumerable<GradientStop> stops, float centerX, float centerY,
            float radiusX, float radiusY,
            float focusX, float focusY)
        {
            return new RadialGradientBrush(Target, stops, new RawVector2(centerX, centerY),
                new RawVector2(radiusX, radiusY), new RawVector2(focusX, focusY));
        }

        public override IPen CreatePen(float width, IBrush brush, IEnumerable<float> dashes)
        {
            return new Pen(width, brush as Brush, dashes, Target);
        }

        public override ITextLayout CreateTextLayout()
        {
            return new DirectWriteTextLayout(this);
        }

        public override IGeometry CreateGeometry()
        {
            return new Geometry(Target);
        }

        public override IGeometry CreateEllipseGeometry(float cx, float cy, float rx, float ry)
        {
            return new Geometry(Target, 
                new D2D.EllipseGeometry(Factory2D, 
                new D2D.Ellipse(new RawVector2(cx, cy), rx, ry)));
        }

        public override IGeometry CreateRectangleGeometry(float x, float y, float w, float h)
        {
            return new Geometry(Target,
                new D2D.RectangleGeometry(Factory2D,
                new RawRectangleF(x, y, x + w, y + h)));
        }

        public override IGeometry CreateGeometryGroup(params IGeometry[] geometries)
        {
            return new Geometry(Target,
                new D2D.GeometryGroup(Factory2D,
                D2D.FillMode.Alternate, geometries.Select(g => (D2D.Geometry)g).ToArray()));
        }

        public override void Dispose()
        {
            Target.Dispose();
            FactoryDW.Dispose();
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
                            Target.FillEllipse(
                                new D2D.Ellipse(
                                    new RawVector2(
                                        ellipse.CenterX,
                                        ellipse.CenterY),
                                    ellipse.RadiusX, ellipse.RadiusY),
                                brush);
                        else
                            Target.DrawEllipse(
                                new D2D.Ellipse(
                                    new RawVector2(
                                        ellipse.CenterX,
                                        ellipse.CenterY),
                                    ellipse.RadiusX, ellipse.RadiusY),
                                pen.Brush, pen.Width, pen.Style);
                        break;
                    case GeometryRenderCommand geometry:
                        if (geometry.Fill)
                            Target.FillGeometry(
                                geometry.Geometry as Geometry,
                                brush);
                        else
                            Target.DrawGeometry(
                                geometry.Geometry as Geometry,
                                pen.Brush, pen.Width, pen.Style);
                        break;
                    case RectangleRenderCommand rect:
                        if (rect.Fill)
                            Target.FillRectangle(
                                new RawRectangleF(
                                    rect.Left,
                                    rect.Top,
                                    rect.Left + rect.Width,
                                    rect.Top + rect.Width),
                                brush);
                        else
                            Target.DrawRectangle(
                                new RawRectangleF(
                                    rect.Left,
                                    rect.Top,
                                    rect.Left + rect.Width,
                                    rect.Top + rect.Width),
                                pen.Brush, pen.Width, pen.Style);
                        break;
                }
            }

            if (command is TransformRenderCommand transformCommand)
            {
                if (transformCommand.Absolute)
                    Target.Transform = transformCommand.Transform.Convert();
                else
                    Target.Transform = transformCommand.Transform.Convert() * Target.Transform;
            }
        }

        protected override void Begin(object ctx)
        {
            Target.BeginDraw();
        }

        protected override void End()
        {
            Target.EndDraw();
        }
    }
}