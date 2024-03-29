﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Effects;
using Rain.Core.Model.Geometry;
using Rain.Core.Model.Imaging;
using Rain.Core.Model.Paint;
using Rain.Core.Model.Text;

namespace Rain.Core
{
    public abstract class RenderContext : IRenderContext
    {
        #region IRenderContext Members

        public virtual IPen CreatePen(float width, IBrush brush)
        {
            return CreatePen(width, brush, Enumerable.Empty<float>());
        }

        public virtual void DrawBitmap(IRenderImage bitmap)
        {
            DrawBitmap(bitmap, new RectangleF(0, 0, bitmap.Width, bitmap.Height), ScaleMode.Linear);
        }

        public virtual void DrawCircle(Vector2 c, float r, IPen pen) { DrawEllipse(c, r, r, pen); }

        public virtual void DrawCircle(float cx, float cy, float r, IPen pen) { DrawEllipse(cx, cy, r, r, pen); }

        public virtual void DrawEllipse(Vector2 c, float rx, float ry, IPen pen) { DrawEllipse(c.X, c.Y, rx, ry, pen); }

        public virtual void DrawEllipse(float cx, float cy, float rx, float ry, IPen pen)
        {
            DrawEllipse(cx, cy, rx, ry, pen, pen.Width);
        }

        public virtual void DrawRectangle(RectangleF rect, IPen pen)
        {
            DrawRectangle(rect.Left, rect.Top, rect.Width, rect.Height, pen);
        }

        public virtual void DrawRectangle(float left, float top, float width, float height, IPen pen)
        {
            DrawRectangle(new RectangleF(left, top, width, height), pen, pen.Width);
        }

        public virtual void FillCircle(Vector2 c, float r, IBrush brush) { FillEllipse(c, r, r, brush); }

        public virtual void FillCircle(float cx, float cy, float r, IBrush brush) { FillEllipse(cx, cy, r, r, brush); }

        public virtual void FillEllipse(Vector2 c, float rx, float ry, IBrush brush)
        {
            FillEllipse(c.X, c.Y, rx, ry, brush);
        }

        public virtual void FillRectangle(RectangleF rect, IBrush brush)
        {
            FillRectangle(rect.Left, rect.Top, rect.Width, rect.Height, brush);
        }

        public abstract void Begin(object ctx);

        public abstract void Clear(Color color);

        public abstract ISolidColorBrush CreateBrush(Color color);

        public abstract ILinearGradientBrush CreateBrush(
            IEnumerable<GradientStop> stops, float startX, float startY, float endX, float endY);

        public abstract IRadialGradientBrush CreateBrush(
            IEnumerable<GradientStop> stops, float centerX, float centerY, float radiusX, float radiusY, float focusX,
            float focusY);

        public abstract T CreateEffect<T>() where T : class, IEffect;

        /// <inheritdoc />
        public abstract IEffectLayer CreateEffectLayer();

        public abstract IGeometry CreateEllipseGeometry(float cx, float cy, float rx, float ry);

        public abstract IFontSource CreateFontSource();

        public abstract IGeometry CreateGeometry();

        public abstract IGeometry CreateGeometryGroup(params IGeometry[] geometries);

        public abstract IPen CreatePen(float width, IBrush brush, IEnumerable<float> dashes);

        public abstract IPen CreatePen(
            float width, IBrush brush, IEnumerable<float> dashes, float dashOffset, LineCap lineCap, LineJoin lineJoin,
            float miterLimit);

        public abstract IGeometry CreateRectangleGeometry(float x, float y, float w, float h);

        public abstract ITextLayout CreateTextLayout();

        public abstract void Dispose();

        public abstract void DrawBitmap(IRenderImage bitmap, RectangleF dstRect, ScaleMode scaleMode);

        /// <inheritdoc />
        public abstract void DrawEffectLayer(IEffectLayer layer);

        /// <inheritdoc />
        public abstract void DrawEllipse(float cx, float cy, float rx, float ry, IPen pen, float penWidth);

        public abstract void DrawGeometry(IGeometry geometry, IPen pen);

        public abstract void DrawGeometry(IGeometry geometry, IPen pen, float width);

        public abstract void DrawLine(Vector2 v1, Vector2 v2, IPen pen);

        /// <inheritdoc />
        public abstract void DrawRectangle(RectangleF rectangleF, IPen pen, float penWidth);

        public void DrawRectangle(Vector2 center, Vector2 radii, IPen pen)
        {
            DrawRectangle(center.X - radii.X, center.Y - radii.Y, radii.X * 2, radii.Y * 2, pen);
        }

        public abstract void End();

        public abstract void FillEllipse(float cx, float cy, float rx, float ry, IBrush brush);

        public abstract void FillGeometry(IGeometry geometry, IBrush brush);

        public abstract void FillRectangle(float left, float top, float width, float height, IBrush brush);

        public void FillRectangle(Vector2 center, Vector2 radii, IBrush brush)
        {
            FillRectangle(center.X - radii.X, center.Y - radii.Y, radii.X * 2, radii.Y * 2, brush);
        }

        public abstract float GetDpi();

        public abstract IRenderImage GetRenderImage(IImageFrame image);

        public abstract IRenderImage GetRenderImage(IImageFrame image, Vector2 scale, ScaleMode mode);


        public abstract void Transform(Matrix3x2 transform, bool absolute = false);
        public abstract float Height { get; }

        public abstract float Width { get; }

        #endregion
    }
}