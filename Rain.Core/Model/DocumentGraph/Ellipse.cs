using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Geometry;
using Rain.Core.Utility;

namespace Rain.Core.Model.DocumentGraph
{
    public class Ellipse : Shape
    {
        public float CenterX
        {
            get => Get<float>();
            set
            {
                Set(value);
                RaiseGeometryChanged();
            }
        }

        public float CenterY
        {
            get => Get<float>();
            set
            {
                Set(value);
                RaiseGeometryChanged();
            }
        }

        public override string DefaultName => "Ellipse";

        public float RadiusX
        {
            get => Get<float>();
            set
            {
                Set(value);
                RaiseGeometryChanged();
            }
        }

        public float RadiusY
        {
            get => Get<float>();
            set
            {
                Set(value);
                RaiseGeometryChanged();
            }
        }

        public override RectangleF GetBounds(IArtContext ctx)
        {
            return new RectangleF(CenterX - RadiusX, CenterY - RadiusY, RadiusX * 2, RadiusY * 2);
        }

        public override IGeometry GetGeometry(IArtContext ctx)
        {
            return ctx.RenderContext.CreateEllipseGeometry(
                CenterX,
                CenterY,
                RadiusX,
                RadiusY);
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
                target.FillEllipse(CenterX, CenterY, RadiusX, RadiusY, cache.GetFill(this));

            if (Stroke?.Brush != null)
            {
                var pen = cache.GetStroke(this);
                target.DrawEllipse(CenterX, CenterY, RadiusX, RadiusY, pen, pen.Width * view.Zoom);
            }

            target.Transform(MathUtils.Invert(transform));
        }
    }
}