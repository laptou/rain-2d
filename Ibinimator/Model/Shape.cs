using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            element.SetAttributeValue("cx", RadiusX);
            element.SetAttributeValue("cy", RadiusY);
            element.SetAttributeValue("rx", RadiusX);
            element.SetAttributeValue("ry", RadiusY);

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

            element.SetAttributeValue("x", 0);
            element.SetAttributeValue("y", 0);
            element.SetAttributeValue("width", Width);
            element.SetAttributeValue("height", Height);

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
            StrokeDashes = new ObservableCollection<float>(new float[] {0, 0, 0, 0});
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

        public FillMode FillMode
        {
            get => Get<FillMode>();
            set
            {
                Set(value);
                RaisePropertyChanged("Geometry");
            }
        }

        [XmlElement]
        public BrushInfo StrokeBrush
        {
            get => Get<BrushInfo>();
            set => Set(value);
        }

        public ObservableCollection<float> StrokeDashes
        {
            get => Get<ObservableCollection<float>>();
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

            if (FillBrush != null)
            {
                element.SetAttributeValue("fill", FillBrush.GetReference());
                element.SetAttributeValue("fill-opacity", FillBrush.Opacity);
            }
            if (StrokeBrush != null)
            {
                element.SetAttributeValue("stroke", StrokeBrush.GetReference());
                element.SetAttributeValue("stroke-opacity", StrokeBrush.Opacity);
            }

            element.SetAttributeValue("stroke-width", StrokeWidth);
            element.SetAttributeValue("vector-effect", "non-scaling-stroke");

            if (StrokeStyle.DashStyle != DashStyle.Solid)
                element.SetAttributeValue(
                    "stroke-dasharray",
                    string.Join(
                        ", ",
                        StrokeDashes.Select(d => d * StrokeWidth)));

            switch (StrokeStyle.LineJoin)
            {
                case LineJoin.Miter:
                    element.SetAttributeValue("stroke-linejoin", "miter");
                    break;
                case LineJoin.Bevel:
                    element.SetAttributeValue("stroke-linejoin", "bevel");
                    break;
                case LineJoin.Round:
                    element.SetAttributeValue("stroke-linejoin", "round");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (StrokeStyle.StartCap)
            {
                case CapStyle.Flat:
                    element.SetAttributeValue("stroke-linecap", "butt");
                    break;
                case CapStyle.Square:
                    element.SetAttributeValue("stroke-linecap", "square");
                    break;
                case CapStyle.Round:
                    element.SetAttributeValue("stroke-linecap", "round");
                    break;
                case CapStyle.Triangle:
                    // warning: not part of SVG standard
                    element.SetAttributeValue("stroke-linecap", "triangle");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

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