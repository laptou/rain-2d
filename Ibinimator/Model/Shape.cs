using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Diagnostics;
using Ibinimator.View.Control;
using Ibinimator.Shared;

namespace Ibinimator.Model
{
    public class Ellipse : Shape
    {
        #region Properties

        public override String DefaultName => "Ellipse";
        public float RadiusX { get => Width / 2; set => Width = value * 2; }
        public float RadiusY { get => Height / 2; set => Height = value * 2; }

        #endregion Properties

        #region Methods

        public override Geometry GetGeometry(Factory factory)
        {
            return new EllipseGeometry(
                factory,
                new SharpDX.Direct2D1.Ellipse(
                    new Vector2(RadiusX, RadiusY),
                    RadiusX,
                    RadiusY));
        }

        #endregion Methods
    }

    public class Rectangle : Shape
    {
        #region Properties

        public override String DefaultName => "Rectangle";

        #endregion Properties

        #region Methods

        public override Geometry GetGeometry(Factory factory)
        {
            return new RectangleGeometry(
                factory,
                new RectangleF(0, 0, Width, Height));
        }

        #endregion Methods
    }

    public abstract class Shape : Layer
    {
        #region Properties

        public override String DefaultName => "Shape";
        public BrushInfo FillBrush { get => Get<BrushInfo>(); set => Set(value); }
        public BrushInfo StrokeBrush { get => Get<BrushInfo>(); set => Set(value); }
        public StrokeStyleProperties StrokeStyle { get => Get<StrokeStyleProperties>(); set => Set(value); }
        public float StrokeWidth { get => Get<float>(); set => Set(value); }
        public override float Height { get => Get<float>(); set => Set(value); }
        public override float Width { get => Get<float>(); set => Set(value); }

        #endregion Properties

        #region Methods

        public RectangleF GetTransformedBounds(Factory factory)
        {
            using (var geom = GetTransformedGeometry(factory))
                return RectangleF.Union(base.GetTransformedBounds(), geom.GetBounds().Convert());
        }

        public abstract Geometry GetGeometry(Factory factory);

        public virtual TransformedGeometry GetTransformedGeometry(Factory factory)
        {
            return new TransformedGeometry(factory, GetGeometry(factory), Transform);
        }

        public override Layer Hit(Factory factory, Vector2 point, Matrix3x2 world)
        {
            var hit = base.Hit(factory, point, world);

            if (hit != null) return hit;

            using (var geometry = GetTransformedGeometry(factory))
            {
                if (FillBrush != null && geometry.FillContainsPoint(point, world, geometry.FlatteningTolerance))
                    return this;

                if (StrokeBrush != null && geometry.StrokeContainsPoint(point, StrokeWidth, null, world, geometry.FlatteningTolerance))
                    return this;

                return null;
            }
        }

        public override void Render(RenderTarget target, CacheHelper cacheHelper)
        {
            if (FillBrush != null)
                target.FillGeometry(cacheHelper.GetGeometry(this), cacheHelper.GetFill(this));

            if (StrokeBrush != null)
            {
                var stroke = cacheHelper.GetStroke(this, target);

                target.DrawGeometry(
                  cacheHelper.GetGeometry(this),
                  stroke.brush,
                  stroke.width,
                  stroke.style);
            }

            base.Render(target, cacheHelper);
        }

        #endregion Methods
    }
}