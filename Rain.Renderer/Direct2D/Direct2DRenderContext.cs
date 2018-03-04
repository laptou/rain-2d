using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core;
using Rain.Core.Model;
using Rain.Core.Model.Effects;
using Rain.Core.Model.Geometry;
using Rain.Core.Model.Imaging;
using Rain.Core.Model.Paint;
using Rain.Core.Model.Text;
using Rain.Renderer.DirectWrite;
using Rain.Renderer.WIC;

using SharpDX;
using SharpDX.Mathematics.Interop;

using Color = Rain.Core.Model.Color;
using D2D = SharpDX.Direct2D1;
using DW = SharpDX.DirectWrite;
using Matrix3x2 = System.Numerics.Matrix3x2;
using RectangleF = Rain.Core.Model.RectangleF;
using Vector2 = System.Numerics.Vector2;

// ReSharper disable InconsistentNaming

namespace Rain.Renderer.Direct2D
{
    public class Direct2DRenderContext : RenderContext
    {
        private readonly Stack<Effect> _effects = new Stack<Effect>();

        private readonly D2D.RenderTarget       _target;
        private readonly D2D.BitmapRenderTarget _virtualTarget;

        public Direct2DRenderContext(D2D.RenderTarget target)
        {
            _target = target;
            _virtualTarget =
                new D2D.BitmapRenderTarget(target,
                                           D2D.CompatibleRenderTargetOptions.None,
                                           _target.Size);
            FactoryDW = new DW.Factory(DW.FactoryType.Shared);
        }

        public D2D.Factory Factory2D => Target.Factory;

        public DW.Factory FactoryDW { get; }

        public override float Height => Target.Size.Height;

        public D2D.RenderTarget Target => _effects.Count > 0 ? _virtualTarget : _target;
        public override float Width => Target.Size.Width;

        /// <inheritdoc />
        public override IFontSource CreateFontSource()
        {
            return new DirectWriteFontSource(FactoryDW);
        }

        public override void Begin(object ctx)
        {
            _target.BeginDraw();
            _virtualTarget.BeginDraw();
            _target.Transform = SharpDX.Matrix3x2.Identity;
            _virtualTarget.Transform = SharpDX.Matrix3x2.Identity;
        }

        public override void Clear(Color color)
        {
            _target.Clear(color.Convert());
            _virtualTarget.Clear(null);
        }


        public override ISolidColorBrush CreateBrush(Color color)
        {
            return new SolidColorBrush(Target, color);
        }

        public override ILinearGradientBrush CreateBrush(
            IEnumerable<GradientStop> stops, float startX, float startY, float endX, float endY)
        {
            return new LinearGradientBrush(Target,
                                           stops,
                                           new RawVector2(startX, startY),
                                           new RawVector2(endX, endY));
        }

        public override IRadialGradientBrush CreateBrush(
            IEnumerable<GradientStop> stops, float centerX, float centerY, float radiusX,
            float radiusY, float focusX, float focusY)
        {
            return new RadialGradientBrush(Target,
                                           stops,
                                           new RawVector2(centerX, centerY),
                                           new RawVector2(radiusX, radiusY),
                                           new RawVector2(focusX, focusY));
        }

        public override T CreateEffect<T>()
        {
            if (typeof(T) == typeof(IGlowEffect))
                return new GlowEffect(_target.QueryInterface<D2D.DeviceContext>()) as T;
            if (typeof(T) == typeof(IDropShadowEffect))
                return new DropShadowEffect(_target.QueryInterface<D2D.DeviceContext>()) as T;
            if (typeof(T) == typeof(IScaleEffect))
                return new ScaleEffect(_target.QueryInterface<D2D.DeviceContext>()) as T;

            return default;
        }

        public override IGeometry CreateEllipseGeometry(float cx, float cy, float rx, float ry)
        {
            return new Geometry(Target,
                                new D2D.EllipseGeometry(Factory2D,
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
            if (geometries.Length == 0)
                return new NullGeometry();

            return new Geometry(Target, geometries);
        }


        public override IPen CreatePen(float width, IBrush brush, IEnumerable<float> dashes)
        {
            return CreatePen(width, brush, dashes, 0, LineCap.Butt, LineJoin.Miter, 4);
        }

        public override IPen CreatePen(
            float width, IBrush brush, IEnumerable<float> dashes, float dashOffset, LineCap lineCap,
            LineJoin lineJoin, float miterLimit)
        {
            return new Pen(width,
                           brush as Brush,
                           dashes,
                           dashOffset,
                           lineCap,
                           lineJoin,
                           miterLimit,
                           Target);
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

        /// <inheritdoc />
        public override void DrawBitmap(IRenderImage img, RectangleF dstRect, ScaleMode scaleMode)
        {
            if (!(img is Bitmap bitmap)) return;

            Target.QueryInterface<D2D.DeviceContext>()
                  .DrawBitmap(bitmap,
                              dstRect.Convert(),
                              1,
                              (D2D.InterpolationMode) scaleMode,
                              null,
                              null);
        }

        public override void DrawEllipse(float cx, float cy, float rx, float ry, IPen iPen)
        {
            var pen = iPen as Pen;

            if (iPen == null) return;

            Target.DrawEllipse(new D2D.Ellipse(new RawVector2(cx, cy), rx, ry),
                               pen.Brush,
                               pen.Width,
                               pen.Style);
        }

        public override void DrawGeometry(IGeometry geometry, IPen iPen)
        {
            DrawGeometry(geometry, iPen, iPen.Width);
        }

        public override void DrawGeometry(IGeometry geometry, IPen iPen, float width)
        {
            var pen = iPen as Pen;

            if (geometry == null ||
                iPen == null) return;

            Target.DrawGeometry(geometry as Geometry, pen.Brush, width, pen.Style);
        }

        public override void DrawLine(Vector2 v1, Vector2 v2, IPen iPen)
        {
            var pen = iPen as Pen;

            if (iPen == null) return;

            Target.DrawLine(v1.Convert(), v2.Convert(), pen.Brush, pen.Width, pen.Style);
        }

        public override void DrawRectangle(
            float left, float top, float width, float height, IPen iPen)
        {
            var pen = iPen as Pen;
            Target.DrawRectangle(new SharpDX.RectangleF(left, top, width, height),
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
            Target.FillEllipse(new D2D.Ellipse(new RawVector2(cx, cy), rx, ry), brush as Brush);
        }

        public override void FillGeometry(IGeometry geometry, IBrush brush)
        {
            if (geometry == null ||
                brush == null) return;

            Target.FillGeometry(geometry as Geometry, brush as Brush);
        }

        public override void FillRectangle(
            float left, float top, float width, float height, IBrush brush)
        {
            Target.FillRectangle(new SharpDX.RectangleF(left, top, width, height), brush as Brush);
        }

        public override float GetDpi() { return Target.DotsPerInch.Width; }

        /// <inheritdoc />
        public override IRenderImage GetRenderImage(IImageFrame image)
        {
            if (image is ImageFrame wicImageFrame)
                return new Bitmap(_target, wicImageFrame);

            throw new ArgumentException("This render context can only process WIC images.");
        }

        /// <inheritdoc />
        public override IRenderImage GetRenderImage(
            IImageFrame image, Vector2 scale, ScaleMode mode)
        {
            var size = new Size2F((int) (image.Width * scale.X), (int) (image.Height * scale.Y));

            using (var bmp = GetRenderImage(image))
            {
                using (var target =
                    new D2D.BitmapRenderTarget(_target,
                                               D2D.CompatibleRenderTargetOptions.None,
                                               size))
                {
                    using (var effect =
                        new ScaleEffect(_target.QueryInterface<D2D.DeviceContext>()))
                    {
                        var dpi = new Vector2(target.DotsPerInch.Width, target.DotsPerInch.Height);
                        effect.ScaleMode = mode;
                        effect.Factor = scale * dpi / 96f;
                        effect.SetInput(0, bmp);
                        var img = effect.GetOutput();
                        target.BeginDraw();
                        target.QueryInterface<D2D.DeviceContext>().DrawImage(img);
                        target.EndDraw();
                    }

                    return new Bitmap(target.Bitmap);
                }
            }
        }

        public override void PopEffect()
        {
            var d2dEffect = _effects.Pop();

            if (_effects.Count == 0)
            {
                _virtualTarget.TryEndDraw(out _, out _);

                var t = _target.Transform;

                _target.Transform = SharpDX.Matrix3x2.Identity;

                using (var output = d2dEffect.GetOutput())
                {
                    _target.QueryInterface<D2D.DeviceContext>().DrawImage(output);
                }

                _target.Transform = t;

                _virtualTarget.BeginDraw();
                _virtualTarget.Clear(null);
            }
        }

        public override void PushEffect(IEffect effect)
        {
            if (!(effect is Effect fx)) throw new ArgumentException(nameof(effect));

            fx.SetInput(0, new Bitmap(_virtualTarget.Bitmap));

            if (_effects.Count > 0)
                _effects.Peek().SetInput(0, fx);

            _effects.Push(fx);
        }

        public override void Transform(Matrix3x2 transform, bool absolute = false)
        {
            if (absolute)
                _virtualTarget.Transform = _target.Transform = transform.Convert();
            else
                _virtualTarget.Transform =
                    _target.Transform = transform.Convert() * _target.Transform;
        }
    }
}