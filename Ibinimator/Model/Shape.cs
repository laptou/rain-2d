using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Ibinimator.Service;
using Ibinimator.Shared;

namespace Ibinimator.Model
{
    [Serializable]
    [XmlType(nameof(Ellipse))]
    public class Ellipse : Shape
    {
        #region Properties

        public override string DefaultName => "Ellipse";

        [XmlIgnore] // ignore bc redundant to width and height
        public float RadiusX { get => Width / 2; set => Width = value * 2; }

        [XmlIgnore]
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

    [Serializable]
    [XmlType(nameof(Rectangle))]
    public class Rectangle : Shape
    {
        [XmlAttribute]
        public override float Width { get => base.Width; set { base.Width = value; RaisePropertyChanged("Geometry"); } }

        [XmlAttribute]
        public override float Height { get => base.Height; set { base.Height = value; RaisePropertyChanged("Geometry"); } }

        #region Properties

        public override string DefaultName => "Rectangle";

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

    [XmlInclude(typeof(Rectangle))]
    [XmlInclude(typeof(Ellipse))]
    [XmlInclude(typeof(Path))]
    public abstract class Shape : Layer
    {
        protected Shape()
        {
            StrokeStyle = new StrokeStyleProperties1
            {
                TransformType = StrokeTransformType.Fixed
            };
        }

        #region Properties

        public override string DefaultName => "Shape";

        [XmlElement]
        public BrushInfo FillBrush { get => Get<BrushInfo>(); set => Set(value); }

        [XmlElement]
        public BrushInfo StrokeBrush { get => Get<BrushInfo>(); set => Set(value); }

        [XmlElement]
        public StrokeStyleProperties1 StrokeStyle { get => Get<StrokeStyleProperties1>(); set => Set(value); }

        [XmlAttribute]
        [DefaultValue(0)]
        public float StrokeWidth { get => Get<float>(); set => Set(value); }

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