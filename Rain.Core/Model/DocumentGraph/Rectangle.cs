using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Geometry;

namespace Rain.Core.Model.DocumentGraph
{
    public class Rectangle : Shape
    {
        public override string DefaultName => "Rectangle";

        public override float Height
        {
            get => base.Height;
            set
            {
                base.Height = value;
                RaiseGeometryChanged();
            }
        }

        public override float Width
        {
            get => base.Width;
            set
            {
                base.Width = value;
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

        public override RectangleF GetBounds(IArtContext ctx)
        {
            return new RectangleF(X, Y, Width, Height);
        }

        public override IGeometry GetGeometry(IArtContext ctx)
        {
            return ctx.RenderContext.CreateRectangleGeometry(X, Y, Width, Height);
        }
    }
}