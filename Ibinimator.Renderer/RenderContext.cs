using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        protected abstract void Apply(RenderCommand command);

        protected abstract void Begin();
        protected abstract void End();

        public virtual IPen CreatePen(float width, IBrush brush)
        {
            return CreatePen(width, brush, Enumerable.Empty<float>());
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

        public void DrawRectangle(float top, float left, float width, float height, IPen pen)
        {
            _commandQueue.Enqueue(
                new RectangleRenderCommand(
                    top, left, left + width, top + height,
                    false, null, pen));
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

        public void FillRectangle(float top, float left, float width, float height, IBrush brush)
        {
            _commandQueue.Enqueue(
                new RectangleRenderCommand(
                    top, left, left + width, top + height,
                    true, brush, null));
        }

        public void Flush()
        {
            Begin();
            while (_commandQueue.Count > 0)
                Apply(_commandQueue.Dequeue());
            End();
        }

        #region IDisposable Members

        public abstract void Dispose();

        #endregion
    }
}