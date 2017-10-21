using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using SharpDX.Direct2D1;

namespace Ibinimator.Renderer
{
    public abstract class RenderContext : IDisposable
    {
        public abstract void Clear(Color color);

        public abstract IBitmap CreateBitmap(Stream stream);

        public abstract ISolidColorBrush CreateBrush(Color color);

        public abstract ILinearGradientBrush CreateBrush(IEnumerable<GradientStop> stops,
            float startX, float startY,
            float endX, float endY);

        public abstract IRadialGradientBrush CreateBrush(IEnumerable<GradientStop> stops,
            float centerX, float centerY,
            float radiusX, float radiusY,
            float focusX, float focusY);

        public abstract IGeometry CreateEllipseGeometry(float cx, float cy, float rx, float ry);

        public abstract IGeometry CreateGeometry();

        public abstract IGeometry CreateGeometryGroup(params IGeometry[] geometries);

        public abstract IPen CreatePen(float width, IBrush brush, IEnumerable<float> dashes);

        public abstract IGeometry CreateRectangleGeometry(float x, float y, float w, float h);

        public abstract ITextLayout CreateTextLayout();

        public abstract void DrawEllipse(float cx, float cy, float rx, float ry, IPen pen);

        public abstract void DrawGeometry(IGeometry geometry, IPen pen);

        public abstract void DrawLine(Vector2 v1, Vector2 v2, IPen pen);

        public abstract void DrawRectangle(float left, float top, float width, float height, IPen pen);

        public abstract void FillEllipse(float cx, float cy, float rx, float ry, IBrush brush);

        public abstract void FillGeometry(IGeometry geometry, IBrush brush);

        public virtual void FillRectangle(RectangleF rect, IBrush brush)
        {
            FillRectangle(rect.Left, rect.Top, rect.Width, rect.Height, brush);
        }

        public abstract void FillRectangle(float left, float top, float width, float height, IBrush brush);

        public abstract void Transform(Matrix3x2 transform, bool absolute = false);

        public abstract void Begin(object ctx);

        public abstract void End();

        public virtual IPen CreatePen(float width, IBrush brush)
        {
            return CreatePen(width, brush, Enumerable.Empty<float>());
        }

        public virtual void DrawEllipse(Vector2 c, float rx, float ry, IPen pen)
        {
            DrawEllipse(c.X, c.Y, rx, ry, pen);
        }

        public virtual void DrawRectangle(RectangleF rect, IPen pen)
        {
            DrawRectangle(rect.Left, rect.Top, rect.Width, rect.Height, pen);
        }

        public virtual void FillEllipse(Vector2 c, float rx, float ry, IBrush brush)
        {
            FillEllipse(c.X, c.Y, rx, ry, brush);
        }

        #region IDisposable Members

        public abstract void Dispose();

        #endregion

        public abstract void DrawBitmap(IBitmap bitmap);
    }

    internal class LineRenderCommand : GeometricRenderCommand
    {
        public LineRenderCommand(Vector2 v1, Vector2 v2, IPen pen) : base(false, null, pen)
        {
            V1 = v1;
            V2 = v2;
        }

        public Vector2 V1 { get; }
        public Vector2 V2 { get; }
    }

    internal class ClearRenderCommand : RenderCommand
    {
        public ClearRenderCommand(Color color)
        {
            Color = color;
        }

        public Color Color { get; }
    }
}