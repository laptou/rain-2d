using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Diagnostics;

namespace Ibinimator.Model
{
    public class Ellipse : Shape
    {
        #region Properties

        public override String DefaultName => "Ellipse";
        public float CenterX { get => X + RadiusX; }
        public float CenterY { get => Y + RadiusY; }
        public float RadiusX { get => Width / 2; set => Width = value * 2; }
        public float RadiusY { get => Height / 2; set => Height = value * 2; }

        #endregion Properties

        #region Methods

        public override RectangleF GetBounds()
        {
            return new RectangleF(X, Y, Width, Height);
        }

        public override Geometry GetGeometry(Factory factory)
        {
            return new EllipseGeometry(
                factory,
                new SharpDX.Direct2D1.Ellipse(
                    new Vector2(CenterX, CenterY),
                    RadiusX,
                    RadiusY));
        }

        public override void Transform(Matrix3x2 mat)
        {
            base.Transform(mat);

            Width *= mat.ScaleVector.X;
            Height *= mat.ScaleVector.Y;

            if (Height == 0)
                Debugger.Break();

            if (Width < 0)
            {
                Width = -Width;
                X = X - Width;
            }

            if (Height < 0)
            {
                Height = -Height;
                Y = Y - Height;
            }
        }

        #endregion Methods
    }

    public class Rectangle : Shape
    {
        #region Properties

        public override String DefaultName => "Rectangle";

        #endregion Properties

        #region Methods

        public override RectangleF GetBounds()
        {
            return new RectangleF(X, Y, Width, Height);
        }

        public override Geometry GetGeometry(Factory factory)
        {
            return new RectangleGeometry(
                factory,
                GetBounds());
        }

        public override void Transform(Matrix3x2 mat)
        {
            base.Transform(mat);

            Height *= mat.ScaleVector.Y;
            Width *= mat.ScaleVector.X;

            if(Width < 0)
            {
                Width = -Width;
                X = X - Width;
            }

            if (Height < 0)
            {
                Height = -Height;
                Y = Y - Height;
            }
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

        public abstract Geometry GetGeometry(Factory factory);

        public override Layer Hit(Factory factory, Vector2 point)
        {
            var hit = base.Hit(factory, point);

            if (hit != null) return hit;

            using (var geometry = GetGeometry(factory))
            {
                if (FillBrush != null && geometry.FillContainsPoint(point))
                    return this;

                if (StrokeBrush != null && geometry.StrokeContainsPoint(point, StrokeWidth))
                    return this;

                return null;
            }
        }

        #endregion Methods
    }
}