﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Core.Model;

namespace Ibinimator.Renderer.Model
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

        public override RectangleF GetBounds(ICacheManager cache) { return new RectangleF(X, Y, Width, Height); }

        public override IGeometry GetGeometry(ICacheManager cache)
        {
            return cache.Context.RenderContext.CreateRectangleGeometry(X, Y, Width, Height);
        }
    }
}