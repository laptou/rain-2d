using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core;
using SharpDX.Mathematics.Interop;
using D2D = SharpDX.Direct2D1;
using DW = SharpDX.DirectWrite;

namespace Ibinimator.Renderer.Direct2D
{
    public class Direct2DRenderContext : RenderContext
    {
        public Direct2DRenderContext(D2D.RenderTarget target)
        {
            Target = target;
            FactoryDW = new DW.Factory(DW.FactoryType.Shared);
        }

        public D2D.Factory Factory2D => Target.Factory;

        public DW.Factory FactoryDW { get; }

        public D2D.RenderTarget Target { get; }

        public override void Clear(Color color)
        {
            Target.Clear(color.Convert());
        }

        public override IBitmap CreateBitmap(Stream stream)
        {
            return new Bitmap(this, stream);
        }

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

        public override IGeometry CreateEllipseGeometry(float cx, float cy, float rx, float ry)
        {
            return new Geometry(Target,
                new D2D.EllipseGeometry(
                    Factory2D,
                    new D2D.Ellipse(
                        new RawVector2(cx, cy),
                        rx, ry))
                {
                    FlatteningTolerance = 0.01f
                });
        }

        public override IGeometry CreateGeometry()
        {
            return new Geometry(Target);
        }

        public override IGeometry CreateGeometryGroup(params IGeometry[] geometries)
        {
            return new Geometry(Target,
                new D2D.GeometryGroup(Factory2D,
                    D2D.FillMode.Alternate, geometries.Select(g => (D2D.Geometry) g).ToArray()));
        }

        public override IPen CreatePen(float width, IBrush brush, IEnumerable<float> dashes)
        {
            return new Pen(width, brush as Brush, dashes, Target);
        }

        public override IGeometry CreateRectangleGeometry(float x, float y, float w, float h)
        {
            return new Geometry(Target,
                new D2D.RectangleGeometry(Factory2D,
                    new RawRectangleF(x, y, x + w, y + h)));
        }

        public override ITextLayout CreateTextLayout()
        {
            return new DirectWriteTextLayout(this);
        }

        public override void Dispose()
        {
            Target.Dispose();
            FactoryDW.Dispose();
        }

        public override void DrawEllipse(float cx, float cy, float rx, float ry, IPen iPen)
        {
            var pen = iPen as Pen;

            Target.DrawEllipse(
                new D2D.Ellipse(
                    new RawVector2(
                        cx,
                        cy),
                    rx, ry),
                pen.Brush, pen.Width, pen.Style);
        }

        public override void DrawGeometry(IGeometry geometry, IPen iPen)
        {
            var pen = iPen as Pen;

            Target.DrawGeometry(
                geometry as Geometry,
                pen.Brush, pen.Width, pen.Style);
        }

        public override void DrawLine(Vector2 v1, Vector2 v2, IPen iPen)
        {
            var pen = iPen as Pen;

            Target.DrawLine(
                v1.Convert(),
                v2.Convert(),
                pen.Brush,
                pen.Width,
                pen.Style);
        }

        public override void DrawRectangle(float left, float top, float width, float height, IPen iPen)
        {
            var pen = iPen as Pen;
            Target.DrawRectangle(
                new RectangleF(
                    left,
                    top,
                    width,
                    height),
                pen.Brush, pen.Width, pen.Style);
        }

        public override void FillEllipse(float cx, float cy, float rx, float ry, IBrush brush)
        {
            Target.FillEllipse(
                new D2D.Ellipse(
                    new RawVector2(
                        cx,
                        cy),
                    rx, ry),
                brush as Brush);
        }

        public override void FillGeometry(IGeometry geometry, IBrush brush)
        {
            Target.FillGeometry(
                geometry as Geometry,
                brush as Brush);
        }

        public override void FillRectangle(float left, float top, float width, float height, IBrush brush)
        {
            Target.FillRectangle(
                new RectangleF(
                    left,
                    top,
                    width,
                    height),
                brush as Brush);
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override void Transform(Matrix3x2 transform, bool absolute = false)
        {
            if (absolute)
                Target.Transform = transform.Convert();
            else
                Target.Transform = transform.Convert() * Target.Transform;
        }

        public override void Begin(object ctx)
        {
            Target.BeginDraw();
        }

        public override void End()
        {
            Target.EndDraw();
        }
    }
}