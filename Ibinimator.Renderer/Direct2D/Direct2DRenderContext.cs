using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Model;
using SharpDX.Mathematics.Interop;
using D2D = SharpDX.Direct2D1;
using DW = SharpDX.DirectWrite;
// ReSharper disable InconsistentNaming

namespace Ibinimator.Renderer.Direct2D
{
    public class Direct2DRenderContext : RenderContext
    {
        private readonly Stack<D2D.Effect> _effects = new Stack<D2D.Effect>();
        private readonly D2D.BitmapRenderTarget _virtualTarget;

        public Direct2DRenderContext(D2D.RenderTarget target)
        {
            _target = target;
            _virtualTarget =
                new D2D.BitmapRenderTarget(target,
                                           D2D.CompatibleRenderTargetOptions.None);
            FactoryDW = new DW.Factory(DW.FactoryType.Shared);
        }

        public D2D.Factory Factory2D => Target.Factory;

        public DW.Factory FactoryDW { get; }

        private readonly D2D.RenderTarget _target;

        public D2D.RenderTarget Target => _effects.Count > 0 ? _virtualTarget : _target;

        public override void Begin(object ctx)
        {
            _target.BeginDraw(); 
            _virtualTarget.BeginDraw();
        }

        public override void Clear(Color color)
        {
            _target.Clear(color.Convert());
            _virtualTarget.Clear(null);
        }

        public override IBitmap CreateBitmap(Stream stream) { return new Bitmap(this, stream); }

        public override ISolidColorBrush CreateBrush(Color color) { return new SolidColorBrush(Target, color); }

        public override ILinearGradientBrush CreateBrush(
            IEnumerable<GradientStop> stops,
            float startX,
            float startY,
            float endX,
            float endY)
        {
            return new LinearGradientBrush(Target, stops, new RawVector2(startX, startY), new RawVector2(endX, endY));
        }

        public override IRadialGradientBrush CreateBrush(
            IEnumerable<GradientStop> stops,
            float centerX,
            float centerY,
            float radiusX,
            float radiusY,
            float focusX,
            float focusY)
        {
            return new RadialGradientBrush(Target,
                                           stops,
                                           new RawVector2(centerX, centerY),
                                           new RawVector2(radiusX, radiusY),
                                           new RawVector2(focusX, focusY));
        }

        public override IGeometry CreateEllipseGeometry(float cx, float cy, float rx, float ry)
        {
            return new Geometry(Target,
                                new D2D.EllipseGeometry(
                                    Factory2D,
                                    new D2D.Ellipse(
                                        new RawVector2(cx, cy),
                                        rx,
                                        ry))
                                {
                                    FlatteningTolerance = 0.01f
                                });
        }

        public override IGeometry CreateGeometry() { return new Geometry(Target); }

        public override IGeometry CreateGeometryGroup(params IGeometry[] geometries)
        {
            return new Geometry(Target, geometries);
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

        public override ITextLayout CreateTextLayout() { return new DirectWriteTextLayout(this); }

        public override void Dispose()
        {
            Target.Dispose();
            FactoryDW.Dispose();
        }

        public override float GetDpi() { return Target.DotsPerInch.Width; }

        public override void PopEffect()
        {
            _virtualTarget.Flush();

            var d2dEffect = _effects.Pop();

            if (_effects.Count > 0)
                _effects.Peek().SetInput(0, d2dEffect.Output, false);
            else
            {
                _target.QueryInterface<D2D.DeviceContext>().DrawImage(d2dEffect);
                _target.QueryInterface<D2D.DeviceContext>().DrawImage(_virtualTarget.Bitmap);
            }
        }

        public override void PushEffect(object effect)
        {
            if (!(effect is D2D.Effect)) throw new ArgumentException(nameof(effect));

            _virtualTarget.Flush();

            var d2dEffect = (D2D.Effect) effect;

            d2dEffect.SetInput(0, _virtualTarget.Bitmap, true);

            if(_effects.Count > 0)
                _effects.Peek().SetInputEffect(0, d2dEffect);

            _effects.Push(d2dEffect);
        }
        public override float Height => Target.Size.Height;
        public override float Width => Target.Size.Width;

        public override void DrawBitmap(IBitmap iBitmap)
        {
            if (!(iBitmap is Bitmap bitmap)) return;

            Target.DrawBitmap(
                bitmap,
                new RawRectangleF(0, 0, bitmap.Width, bitmap.Height), 1,
                D2D.BitmapInterpolationMode.Linear);
        }

        public override void DrawEllipse(float cx, float cy, float rx, float ry, IPen iPen)
        {
            var pen = iPen as Pen;

            if (iPen == null) return;

            Target.DrawEllipse(
                new D2D.Ellipse(
                    new RawVector2(
                        cx,
                        cy),
                    rx,
                    ry),
                pen.Brush,
                pen.Width,
                pen.Style);
        }

        public override void DrawGeometry(IGeometry geometry, IPen iPen)
        {
            var pen = iPen as Pen;

            if (geometry == null || iPen == null) return;

            Target.DrawGeometry(
                geometry as Geometry,
                pen.Brush,
                pen.Width,
                pen.Style);
        }

        public override void DrawLine(Vector2 v1, Vector2 v2, IPen iPen)
        {
            var pen = iPen as Pen;

            if (iPen == null) return;

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
                pen.Brush,
                pen.Width,
                pen.Style);
        }

        public override void End()
        {
            _virtualTarget.EndDraw();
            _target.EndDraw();
        }

        public override void FillEllipse(float cx, float cy, float rx, float ry, IBrush brush)
        {
            Target.FillEllipse(
                new D2D.Ellipse(
                    new RawVector2(
                        cx,
                        cy),
                    rx,
                    ry),
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

        public override void Transform(Matrix3x2 transform, bool absolute = false)
        {
            if (absolute)
                Target.Transform = transform.Convert();
            else
                Target.Transform = transform.Convert() * Target.Transform;
        }
    }
}