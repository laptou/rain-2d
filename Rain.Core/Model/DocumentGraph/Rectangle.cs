using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Geometry;
using Rain.Core.Model.Paint;
using Rain.Core.Utility;

namespace Rain.Core.Model.DocumentGraph
{
    public class Rectangle : Shape
    {
        public override string DefaultName => "Rectangle";

        public float Height
        {
            get => Get<float>();
            set
            {
                Set(value);
                RaiseGeometryChanged();
            }
        }

        public float Width
        {
            get => Get<float>();
            set
            {
                Set(value);
                RaiseGeometryChanged();
            }
        }

        public float X
        {
            get => Get<float>();
            set
            {
                Set(value);
                RaiseGeometryChanged();
            }
        }

        public float Y
        {
            get => Get<float>();
            set
            {
                Set(value);
                RaiseGeometryChanged();
            }
        }

        public override RectangleF GetBounds(IArtContext ctx) { return new RectangleF(X, Y, Width, Height); }

        public override IGeometry GetGeometry(IArtContext ctx)
        {
            return ctx.RenderContext.CreateRectangleGeometry(X, Y, Width, Height);
        }

        /// <inheritdoc />
        public override void Render(IRenderContext target, ICacheManager cache, IViewManager view)
        {
            if (!Visible) return;

            // grabbing the value here avoids jitter if the transform is changed during the rendering
            var transform = Transform;

            target.Transform(transform);

            // we could get away with just not overriding this method
            // but using specific Fill<Shape> should be faster than using
            // FillGeometry
            if (Fill != null)
                target.FillRectangle(new RectangleF(X, Y, Width, Height), cache.GetBrush(Fill));

            if (Stroke?.Brush != null)
            {
                var pen = cache.GetPen(Stroke);
                target.DrawRectangle(new RectangleF(X, Y, Width, Height), pen, pen.Width * view.Zoom);
            }

            target.Transform(MathUtils.Invert(transform));
        }
    }
}