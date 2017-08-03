using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Diagnostics;
using Ibinimator.View.Control;
using Ibinimator.Shared;
using System.ComponentModel;
using Ibinimator.Service;

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
        public override float Width { get => base.Width; set { base.Width = value; RaisePropertyChanged("Geometry"); } }
        public override float Height { get => base.Height; set { base.Height = value; RaisePropertyChanged("Geometry"); } }

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
        public Shape()
        {
            StrokeStyle = new StrokeStyleProperties1()
            {
                TransformType = StrokeTransformType.Fixed
            };
        }

        #region Properties

        public override String DefaultName => "Shape";
        public BrushInfo FillBrush { get => Get<BrushInfo>(); set => Set(value); }
        public BrushInfo StrokeBrush { get => Get<BrushInfo>(); set => Set(value); }
        public StrokeStyleProperties1 StrokeStyle { get => Get<StrokeStyleProperties1>(); set => Set(value); }
        public float StrokeWidth { get => Get<float>(); set => Set(value); }

        public override float Height { get => Get<float>(); set => Set(value); }
        public override float Width { get => Get<float>(); set => Set(value); }

        #endregion Properties

        #region Methods

        public abstract Geometry GetGeometry(Factory factory);

        public override T Hit<T>(Factory factory, Vector2 point, Matrix3x2 world)
        {
            var hit = base.Hit<T>(factory, point, world);

            if (hit != null) return hit;

            if (this is T)
            {
                using (var geometry = GetGeometry(factory))
                {
                    if (FillBrush != null &&
                        geometry.FillContainsPoint(point, AbsoluteTransform, geometry.FlatteningTolerance))
                        return this as T;

                    if (StrokeBrush != null && geometry.StrokeContainsPoint(point, StrokeWidth, null, AbsoluteTransform,
                            geometry.FlatteningTolerance))
                        return this as T;
                }
            }

            return null;
        }

        public override void Render(RenderTarget target, ICacheManager cacheHelper)
        {
            target.Transform *= AbsoluteTransform;

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

            target.Transform *= Matrix3x2.Invert(AbsoluteTransform);

            base.Render(target, cacheHelper);
        }

        #endregion Methods
    }
}