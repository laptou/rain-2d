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

        public override Geometry GetGeometry(ICacheManager cache)
        {
            return new EllipseGeometry(
                cache.ArtView.Direct2DFactory,
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

        public override float Height
        {
            get => base.Height;
            set
            {
                base.Height = value;
                RaisePropertyChanged("Geometry");
            }
        }

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

        public override Geometry GetGeometry(ICacheManager cache)
        {
            return new RectangleGeometry(
                cache.ArtView.Direct2DFactory,
                new RectangleF(0, 0, Width, Height));
        }
    }

    public abstract class Shape : Layer, IGeometricLayer
    {
        protected Shape()
        {
            StrokeInfo = new StrokeInfo();
        }

        public override string DefaultName => "Shape";

        [Undoable]
        public BrushInfo FillBrush
        {
            get => Get<BrushInfo>();
            set => Set(value);
        }

        [Undoable]
        public FillMode FillMode
        {
            get => Get<FillMode>();
            set
            {
                Set(value);
                RaisePropertyChanged("Geometry");
            }
        }

        [Undoable]
        public BrushInfo StrokeBrush
        {
            get => Get<BrushInfo>();
            set => Set(value);
        }

        [Undoable]
        public StrokeInfo StrokeInfo
        {
            get => Get<StrokeInfo>();
            set => Set(value);
        }

        public abstract Geometry GetGeometry(ICacheManager factory);

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

            element.SetAttributeValue("stroke-width", StrokeInfo.Width);
            element.SetAttributeValue("vector-effect", "non-scaling-stroke");

            if (StrokeInfo.Style.DashStyle != DashStyle.Solid)
                element.SetAttributeValue(
                    "stroke-dasharray",
                    string.Join(
                        ", ",
                        StrokeInfo.Dashes.Select(d => d * StrokeInfo.Width)));

            switch (StrokeInfo.Style.LineJoin)
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

            switch (StrokeInfo.Style.StartCap)
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

        public override T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe)
        {
            if (!(this is T)) return null;

            point = Matrix3x2.TransformPoint(Matrix3x2.Invert(Transform), point);

            var bounds = cache.GetBounds(this);

            if (!bounds.Contains(point)) return null;

            var geometry = cache.GetGeometry(this);

            if (FillBrush != null &&
                geometry.FillContainsPoint(
                    point,
                    Matrix3x2.Identity,
                    geometry.FlatteningTolerance))
                return this as T;

            if (StrokeBrush != null &&
                geometry.StrokeContainsPoint(
                    point,
                    StrokeInfo.Width,
                    null,
                    Matrix3x2.Identity,
                    geometry.FlatteningTolerance))
                return this as T;

            return null;
        }

        public override void Render(RenderTarget target, ICacheManager cache)
        {
            target.Transform = Transform * target.Transform;

            var dc = target.QueryInterfaceOrNull<DeviceContext1>();

            if (FillBrush != null)
            {
                if (dc != null)
                    dc.DrawGeometryRealization(cache.GetFillGeometry(this), cache.GetFill(this));
                else
                    target.FillGeometry(cache.GetGeometry(this), cache.GetFill(this));
            }

            if (StrokeBrush != null)
            {
                var stroke = cache.GetStroke(this);

                if (dc != null)
                    dc.DrawGeometryRealization(cache.GetStrokeGeometry(this), stroke.Brush);
                else
                    target.DrawGeometry(
                        cache.GetGeometry(this),
                        stroke.Brush,
                        stroke.Width,
                        stroke.Style);
            }

            target.Transform = Matrix3x2.Invert(Transform) * target.Transform;
        }
    }
}