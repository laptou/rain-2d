using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using Rain.Core;
using Rain.Core.Model;
using Rain.Core.Model.Effects;
using Rain.Core.Model.Geometry;
using Rain.Core.Model.Paint;

using Color = Rain.Core.Model.Color;
using GradientStop = Rain.Core.Model.Paint.GradientStop;

namespace Rain.Renderer.WPF
{
    public class WpfRenderContext : RenderContext
    {
        private readonly Queue<RenderCommand> _commandQueue = new Queue<RenderCommand>();
        private          DrawingContext       _ctx;

        public override float Height { get; }
        public override float Width { get; }

        public override void Begin(object ctx)
        {
            if (ctx is DrawingContext dc)
                _ctx = dc;
        }

        public override void Clear(Color color)
        {
            _commandQueue.Enqueue(new ClearRenderCommand(color));
        }

        public override IBitmap CreateBitmap(Stream stream) { throw new NotImplementedException(); }

        public override ISolidColorBrush CreateBrush(Color color)
        {
            return new SolidColorBrush(color);
        }

        public override ILinearGradientBrush CreateBrush(
            IEnumerable<GradientStop> stops, float startX, float startY, float endX, float endY)
        {
            return new LinearGradientBrush(stops, new Point(startX, startY), new Point(endX, endY));
        }

        public override IRadialGradientBrush CreateBrush(
            IEnumerable<GradientStop> stops, float centerX, float centerY, float radiusX,
            float radiusY, float focusX, float focusY)
        {
            return new RadialGradientBrush(stops,
                                           new Point(centerX, centerY),
                                           new Size(radiusX, radiusX),
                                           new Point(focusX, focusY));
        }

        public override T CreateEffect<T>() { throw new NotImplementedException(); }

        public override IGeometry CreateEllipseGeometry(float cx, float cy, float rx, float ry)
        {
            return new Geometry(new EllipseGeometry(new Point(cx, cy), rx, ry));
        }

        public override IGeometry CreateGeometry() { return new Geometry(); }

        public override IGeometry CreateGeometryGroup(params IGeometry[] geometries)
        {
            return new Geometry(new GeometryGroup
            {
                Children =
                    new GeometryCollection(
                        geometries.Select(g => (System.Windows.Media.Geometry) g))
            });
        }

        public override IPen CreatePen(float width, IBrush brush, IEnumerable<float> dashes)
        {
            return new Pen(width, brush as Brush, dashes);
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
                           miterLimit);
        }

        public override IGeometry CreateRectangleGeometry(float x, float y, float w, float h)
        {
            return new Geometry(new RectangleGeometry(new Rect(new Point(x, y), new Size(w, h))));
        }

        public override ITextLayout CreateTextLayout() { throw new NotImplementedException(); }

        public override void Dispose() { _ctx = null; }

        public override void DrawBitmap(IBitmap bitmap) { throw new NotImplementedException(); }

        public override void DrawEllipse(float cx, float cy, float rx, float ry, IPen pen)
        {
            _commandQueue.Enqueue(new EllipseRenderCommand(cx, cy, rx, ry, false, null, pen));
        }

        public override void DrawGeometry(IGeometry geometry, IPen pen)
        {
            DrawGeometry(geometry, pen, pen.Width);
        }

        public override void DrawGeometry(IGeometry geometry, IPen pen, float width)
        {
#warning This currently ignores the width parameter. See Apply().
            _commandQueue.Enqueue(new GeometryRenderCommand(geometry, false, null, pen));
        }

        public override void DrawLine(Vector2 v1, Vector2 v2, IPen pen)
        {
            _commandQueue.Enqueue(new LineRenderCommand(v1, v2, pen));
        }

        public override void DrawRectangle(
            float left, float top, float width, float height, IPen pen)
        {
            _commandQueue.Enqueue(
                new RectangleRenderCommand(left, top, height, width, false, null, pen));
        }

        public override void End()
        {
            while (_commandQueue.Count > 0)
                Apply(_commandQueue.Dequeue());

            _ctx = null;
        }

        public override void FillEllipse(float cx, float cy, float rx, float ry, IBrush brush)
        {
            _commandQueue.Enqueue(new EllipseRenderCommand(cx, cy, rx, ry, true, brush, null));
        }

        public override void FillGeometry(IGeometry geometry, IBrush brush)
        {
            _commandQueue.Enqueue(new GeometryRenderCommand(geometry, true, brush, null));
        }

        public override void FillRectangle(RectangleF rect, IBrush brush)
        {
            FillRectangle(rect.Left, rect.Top, rect.Width, rect.Height, brush);
        }

        public override void FillRectangle(
            float left, float top, float width, float height, IBrush brush)
        {
            _commandQueue.Enqueue(
                new RectangleRenderCommand(left, top, width, height, true, brush, null));
        }

        public override float GetDpi() { return 0; }

        public override void PopEffect() { throw new NotImplementedException(); }

        public override void PushEffect(IEffect effect) { throw new NotImplementedException(); }

        public override void Transform(Matrix3x2 transform, bool absolute = false)
        {
            _commandQueue.Enqueue(new TransformRenderCommand(transform, absolute));
        }

        protected void Apply(RenderCommand command)
        {
            switch (command)
            {
                case EllipseRenderCommand ellipse:
                    _ctx?.DrawEllipse(ellipse.Brush as Brush,
                                      ellipse.Pen as Pen,
                                      new Point(ellipse.CenterX, ellipse.CenterY),
                                      ellipse.RadiusX,
                                      ellipse.RadiusY);

                    break;
                case GeometryRenderCommand geometry:
                    _ctx?.DrawGeometry(geometry.Brush as Brush,
                                       geometry.Pen as Pen,
                                       geometry.Geometry as Geometry);

                    break;
                case RectangleRenderCommand rect:
                    _ctx?.DrawRectangle(rect.Brush as Brush,
                                        rect.Pen as Pen,
                                        new Rect(rect.Left, rect.Top, rect.Width, rect.Height));

                    break;
                case TransformRenderCommand transform:
                    _ctx?.PushTransform(new MatrixTransform(transform.Transform.M11,
                                                            transform.Transform.M12,
                                                            transform.Transform.M21,
                                                            transform.Transform.M22,
                                                            transform.Transform.M31,
                                                            transform.Transform.M32));

                    break;
            }
        }
    }
}