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
    public class Direct2DEffectLayer : Direct2DRenderContext, IEffectLayer, IRenderImage
    {
        private readonly Bitmap                 _bmp;
        private readonly D2D.BitmapRenderTarget _target;
        private          Effect                 _effect;

        internal Direct2DEffectLayer(Direct2DRenderContext ctx) : base(CreateTarget(ctx))
        {
            _target = (D2D.BitmapRenderTarget) base.Target;
            _bmp = new Bitmap(_target.Bitmap);
        }

        internal override D2D.RenderTarget Target => _target;


        private static D2D.RenderTarget CreateTarget(Direct2DRenderContext ctx)
        {
            return new D2D.BitmapRenderTarget(ctx.Target, D2D.CompatibleRenderTargetOptions.None);
        }

        #region IEffectLayer Members

        public void ClearEffect() { _effect = null; }

        /// <inheritdoc />
        public override void Dispose()
        {
            _bmp.Dispose();
            base.Dispose();
        }


        public IEffect GetEffect() { return _effect; }

        public void PushEffect(IEffect effect)
        {
            if (!(effect is Effect fx)) throw new ArgumentException(nameof(effect));

            fx.SetInput(0, _bmp);

            if (_effect != null)
                _effect.SetInput(0, fx);
            else
                _effect = fx;
        }

        #endregion

        #region IRenderImage Members

        /// <inheritdoc />
        public T Unwrap<T>() where T : class { return _target.Bitmap as T; }

        /// <inheritdoc />
        public bool Alpha => true;

        /// <inheritdoc />
        public float Dpi => _target.DotsPerInch.Width;

        /// <inheritdoc />
        public int PixelHeight => _target.PixelSize.Height;

        /// <inheritdoc />
        public int PixelWidth => _target.PixelSize.Width;

        #endregion
    }


    public class Direct2DRenderContext : RenderContext
    {
        private readonly D2D.RenderTarget _target;

        public Direct2DRenderContext(D2D.RenderTarget target) : this(target, new DW.Factory(DW.FactoryType.Shared)) { }

        public Direct2DRenderContext(D2D.RenderTarget target, DW.Factory factory)
        {
            _target = target;
            FactoryDW = factory;
        }

        public D2D.Factory FactoryD2D => _target.Factory;

        public DW.Factory FactoryDW { get; }

        public override float Height => _target.Size.Height;

        public override float Width => _target.Size.Width;

        internal virtual D2D.RenderTarget Target => _target;

        public override void Begin(object ctx)
        {
            Target.Transform = SharpDX.Matrix3x2.Identity;

            Target.BeginDraw();
        }

        public override void Clear(Color color) { Target.Clear(color.Convert()); }


        public override ISolidColorBrush CreateBrush(Color color) { return new SolidColorBrush(Target, color); }

        public override ILinearGradientBrush CreateBrush(
            IEnumerable<GradientStop> stops, float startX, float startY, float endX, float endY)
        {
            return new LinearGradientBrush(Target, stops, new RawVector2(startX, startY), new RawVector2(endX, endY));
        }

        public override IRadialGradientBrush CreateBrush(
            IEnumerable<GradientStop> stops, float centerX, float centerY, float radiusX, float radiusY, float focusX,
            float focusY)
        {
            return new RadialGradientBrush(Target,
                                           stops,
                                           new RawVector2(centerX, centerY),
                                           new RawVector2(radiusX, radiusY),
                                           new RawVector2(focusX, focusY));
        }

        public override T CreateEffect<T>()
        {
            using (var dc = Target.QueryInterface<D2D.DeviceContext>())
            {
                if (typeof(T) == typeof(IGlowEffect))
                    return new GlowEffect(dc) as T;
                if (typeof(T) == typeof(IDropShadowEffect))
                    return new DropShadowEffect(dc) as T;
                if (typeof(T) == typeof(IScaleEffect))
                    return new ScaleEffect(dc) as T;
            }

            return default;
        }

        /// <inheritdoc />
        public override IEffectLayer CreateEffectLayer() { return new Direct2DEffectLayer(this); }

        public override IGeometry CreateEllipseGeometry(float cx, float cy, float rx, float ry)
        {
            return new Geometry(Target,
                                new D2D.EllipseGeometry(FactoryD2D, new D2D.Ellipse(new RawVector2(cx, cy), rx, ry))
                                {
                                    FlatteningTolerance = 0.01f
                                });
        }

        /// <inheritdoc />
        public override IFontSource CreateFontSource() { return new DirectWriteFontSource(FactoryDW); }

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
            float width, IBrush brush, IEnumerable<float> dashes, float dashOffset, LineCap lineCap, LineJoin lineJoin,
            float miterLimit)
        {
            return new Pen(width, brush as Brush, dashes, dashOffset, lineCap, lineJoin, miterLimit, _target);
        }

        public override IGeometry CreateRectangleGeometry(float x, float y, float w, float h)
        {
            return new Geometry(Target, new D2D.RectangleGeometry(FactoryD2D, new RawRectangleF(x, y, x + w, y + h)));
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
            var native = img.Unwrap<D2D.Bitmap>();

            if (native == null) return;

            using (var dc = Target.QueryInterface<D2D.DeviceContext>())
            {
                dc.DrawBitmap(native, dstRect.Convert(), 1, (D2D.InterpolationMode) scaleMode, null, null);
            }
        }

        /// <inheritdoc />
        public override void DrawEffectLayer(IEffectLayer layer)
        {
            var native = layer.GetEffect().Unwrap<D2D.Effect>();

            if (native == null) return;

            using (var dc = Target.QueryInterface<D2D.DeviceContext>())
            {
                dc.DrawImage(native);
            }
        }

        /// <inheritdoc />
        public override void DrawEllipse(float cx, float cy, float rx, float ry, IPen iPen, float penWidth)
        {
            if (iPen is Pen pen)
                Target.DrawEllipse(new D2D.Ellipse(new RawVector2(cx, cy), rx, ry), pen.Brush, penWidth, pen.Style);
        }

        public override void DrawGeometry(IGeometry geometry, IPen iPen) { DrawGeometry(geometry, iPen, iPen.Width); }

        public override void DrawGeometry(IGeometry geometry, IPen iPen, float width)
        {
            var pen = iPen as Pen;

            if (geometry == null ||
                iPen == null) return;

            Target.DrawGeometry(geometry as Geometry, pen.Brush, width, pen.Style);
        }

        public override void DrawLine(Vector2 v1, Vector2 v2, IPen iPen)
        {
            var pen = (Pen) iPen;

            if (iPen == null) return;

            Target.DrawLine(v1.Convert(), v2.Convert(), pen.Brush, pen.Width, pen.Style);
        }

        /// <inheritdoc />
        public override void DrawRectangle(RectangleF rect, IPen iPen, float penWidth)
        {
            var pen = (Pen) iPen;

            Target.DrawRectangle(rect.Convert(), pen.Brush, penWidth, pen.Style);
        }

        public override void End() { Target.EndDraw(); }

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

        public override void FillRectangle(float left, float top, float width, float height, IBrush brush)
        {
            Target.FillRectangle(new SharpDX.RectangleF(left, top, width, height), brush as Brush);
        }

        public override float GetDpi() { return Target.DotsPerInch.Width; }

        /// <inheritdoc />
        public override IRenderImage GetRenderImage(IImageFrame image)
        {
            if (image is ImageFrame wicImageFrame)
                return new Bitmap(Target, wicImageFrame);

            throw new ArgumentException("This render context can only process WIC images.");
        }

        /// <inheritdoc />
        public override IRenderImage GetRenderImage(IImageFrame image, Vector2 scale, ScaleMode mode)
        {
            var size = new Size2F((int) (image.Width * scale.X), (int) (image.Height * scale.Y));

            using (var bmp = GetRenderImage(image))
            {
                using (var target = new D2D.BitmapRenderTarget(Target, D2D.CompatibleRenderTargetOptions.None, size))
                {
                    using (var effect = new ScaleEffect(Target.QueryInterface<D2D.DeviceContext>()))
                    {
                        var dpi = new Vector2(target.DotsPerInch.Width, target.DotsPerInch.Height);
                        effect.ScaleMode = mode;
                        effect.Factor = scale * dpi / 96f;
                        effect.SetInput(0, bmp);
                        var img = effect.GetOutput();
                        target.BeginDraw();

                        using (var dc = Target.QueryInterface<D2D.DeviceContext>())
                        {
                            dc.DrawImage(img);
                        }

                        target.EndDraw();
                    }

                    return new Bitmap(target.Bitmap);
                }
            }
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