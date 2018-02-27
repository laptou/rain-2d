using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Geometry;

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
    }
}