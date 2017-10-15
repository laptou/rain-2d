using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
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

        public override Vector2 Origin
        {
            get => new Vector2(CenterX - RadiusX, CenterY - RadiusY);
            set => (CenterX, CenterY) = (value.X + RadiusX, value.Y + RadiusY);
        }

        public float RadiusX
        {
            get => Width / 2;
            set
            {
                Width = value * 2;
                RaiseGeometryChanged();
            }
        }

        public float RadiusY
        {
            get => Height / 2;
            set
            {
                Height = value * 2;
                RaiseGeometryChanged();
            }
        }

        public override RectangleF GetBounds(ICacheManager cache)
        {
            return new RectangleF(CenterX - RadiusX, CenterY - RadiusY, Width, Height);
        }

        public override IGeometry GetGeometry(ICacheManager cache)
        {
            return cache.Context.RenderContext.CreateEllipseGeometry(CenterX, CenterY, RadiusX, RadiusY);
        }
    }
}