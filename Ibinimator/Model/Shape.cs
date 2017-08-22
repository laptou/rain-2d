using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Ibinimator.Service;
using SharpDX;
using SharpDX.Direct2D1;

namespace Ibinimator.Model
{
    [Serializable]
    [XmlType(nameof(Ellipse))]
    public class Ellipse : Shape
    {
        public override string DefaultName => "Ellipse";

        [XmlIgnore] // ignore bc redundant to width and height
        public float RadiusX
        {
            get => Width / 2;
            set => Width = value * 2;
        }

        [XmlIgnore]
        public float RadiusY
        {
            get => Height / 2;
            set => Height = value * 2;
        }

        protected override string ElementName => "ellipse";

        public override XElement GetElement()
        {
            var element = base.GetElement();

            element.Add(new XAttribute("cx", X + RadiusX));
            element.Add(new XAttribute("cy", Y + RadiusY));
            element.Add(new XAttribute("rx", RadiusX));
            element.Add(new XAttribute("ry", RadiusY));

            return element;
        }

        public override Geometry GetGeometry(Factory factory)
        {
            return new EllipseGeometry(
                factory,
                new SharpDX.Direct2D1.Ellipse(
                    new Vector2(RadiusX, RadiusY),
                    RadiusX,
                    RadiusY));
        }
    }

    [Serializable]
    [XmlType(nameof(Rectangle))]
    public class Rectangle : Shape
    {
        public override string DefaultName => "Rectangle";

        [XmlAttribute]
        public override float Height
        {
            get => base.Height;
            set
            {
                base.Height = value;
                RaisePropertyChanged("Geometry");
            }
        }

        [XmlAttribute]
        public override float Width
        {
            get => base.Width;
            set
            {
                base.Width = value;
                RaisePropertyChanged("Geometry");
            }
        }

        protected override string ElementName => "rect";

        public override XElement GetElement()
        {
            var element = base.GetElement();

            element.Add(new XAttribute("x", X));
            element.Add(new XAttribute("y", Y));
            element.Add(new XAttribute("width", Width));
            element.Add(new XAttribute("height", Height));

            return element;
        }

        public override Geometry GetGeometry(Factory factory)
        {
            return new RectangleGeometry(
                factory,
                new RectangleF(0, 0, Width, Height));
        }
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

        public override string DefaultName => "Shape";

        [XmlElement]
        public BrushInfo FillBrush
        {
            get => Get<BrushInfo>();
            set => Set(value);
        }

        [XmlElement]
        public BrushInfo StrokeBrush
        {
            get => Get<BrushInfo>();
            set => Set(value);
        }

        [XmlElement]
        public StrokeStyleProperties1 StrokeStyle
        {
            get => Get<StrokeStyleProperties1>();
            set => Set(value);
        }

        [XmlAttribute]
        [DefaultValue(0)]
        public float StrokeWidth
        {
            get => Get<float>();
            set => Set(value);
        }

        public abstract Geometry GetGeometry(Factory factory);

        public override XElement GetElement()
        {
            var element = base.GetElement();

            element.Add(new XAttribute("fill", FillBrush.GetReference()));
            element.Add(new XAttribute("stroke", StrokeBrush.GetReference()));
            element.Add(new XAttribute("stroke-width", StrokeWidth));

            return element;
        }

        public override T Hit<T>(Factory factory, Vector2 point, Matrix3x2 world, bool includeMe)
        {
            if (this is T)
                using (var geometry = GetGeometry(factory))
                {
                    if (FillBrush != null &&
                        geometry.FillContainsPoint(point, AbsoluteTransform, geometry.FlatteningTolerance))
                        return this as T;

                    if (StrokeBrush != null && geometry.StrokeContainsPoint(point, StrokeWidth, null, AbsoluteTransform,
                            geometry.FlatteningTolerance))
                        return this as T;
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
        }
    }
}