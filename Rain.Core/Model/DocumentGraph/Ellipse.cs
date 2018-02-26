﻿using System;
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

        public override RectangleF GetBounds(IArtContext ctx)
        {
            return new RectangleF(CenterX - RadiusX, CenterY - RadiusY, Width, Height);
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