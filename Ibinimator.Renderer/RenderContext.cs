using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core;
using SharpDX.Direct2D1;

namespace Ibinimator.Renderer
{
    public abstract class RenderContext : IDisposable
    {
        private readonly Queue<RenderCommand> _commandQueue = new Queue<RenderCommand>();

        public abstract ISolidColorBrush CreateBrush(Color color);

        public abstract ILinearGradientBrush CreateBrush(IEnumerable<GradientStop> stops,
            float startX, float startY,
            float endX, float endY);

        public abstract IRadialGradientBrush CreateBrush(IEnumerable<GradientStop> stops,
            float centerX, float centerY,
            float radiusX, float radiusY,
            float focusX, float focusY);

        public abstract IPen CreatePen(float width, IBrush brush, IEnumerable<float> dashes);

        public abstract ITextLayout CreateTextLayout();

        public abstract IGeometry CreateGeometry();

        public abstract IGeometry CreateEllipseGeometry(float cx, float cy, float rx, float ry);

        public abstract IGeometry CreateRectangleGeometry(float x, float y, float w, float h);

        public abstract IGeometry CreateGeometryGroup(params IGeometry[] geometries);

        protected abstract void Apply(RenderCommand command);

        protected abstract void Begin(object ctx);

        protected abstract void End();

        public virtual IPen CreatePen(float width, IBrush brush)
        {
            return CreatePen(width, brush, Enumerable.Empty<float>());
        }

        public void DrawEllipse(Vector2 c, float rx, float ry, IPen pen)
        {
            DrawEllipse(c.X, c.Y, rx, ry, pen);
        }

        public void DrawEllipse(float cx, float cy, float rx, float ry, IPen pen)
        {
            _commandQueue.Enqueue(
                new EllipseRenderCommand(
                    cx, cy, rx, ry,
                    false, null, pen));
        }

        public void DrawGeometry(IGeometry geometry, IPen pen)
        {
            _commandQueue.Enqueue(new GeometryRenderCommand(geometry, false, null, pen));
        }

        public void DrawRectangle(RectangleF rect, IPen pen)
        {
            DrawRectangle(rect.Left, rect.Top, rect.Width, rect.Height, pen);
        }

        public void FillRectangle(RectangleF rect, IBrush brush)
        {
            FillRectangle(rect.Left, rect.Top, rect.Width, rect.Height, brush);
        }

        public void DrawRectangle(float left, float top, float width, float height, IPen pen)
        {
            _commandQueue.Enqueue(
                new RectangleRenderCommand(left, top,
                    top + height, left + width, false, null, pen));
        }

        public void FillEllipse(Vector2 c, float rx, float ry, IBrush brush)
        {
            FillEllipse(c.X, c.Y, rx, ry, brush);
        }
        public void FillEllipse(float cx, float cy, float rx, float ry, IBrush brush)
        {
            _commandQueue.Enqueue(
                new EllipseRenderCommand(
                    cx, cy, rx, ry,
                    true, brush, null));
        }

        public void FillGeometry(IGeometry geometry, IBrush brush)
        {
            _commandQueue.Enqueue(new GeometryRenderCommand(geometry, true, brush, null));
        }

        public void FillRectangle(float left, float top, float width, float height, IBrush brush)
        {
            _commandQueue.Enqueue(
                new RectangleRenderCommand(left, top, width, height, true, brush, null));
        }

        public void Flush()
        {
            Begin(null);
            while (_commandQueue.Count > 0)
                Apply(_commandQueue.Dequeue());
            End();
        }

        public void Transform(Matrix3x2 transform, bool absolute = false)
        {
            _commandQueue.Enqueue(new TransformRenderCommand(transform, absolute));
        }

        #region IDisposable Members

        public abstract void Dispose();

        #endregion

        public void DrawLine(Vector2 v1, Vector2 v2, IPen pen)
        {
            throw new NotImplementedException();
        }

        public void Clear(Color color)
        {
            throw new NotImplementedException();
        }

        public abstract IBitmap CreateBitmap(Stream stream);
    }
}