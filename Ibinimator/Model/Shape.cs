using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
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
        protected override string ElementName { get; }
        public override string DefaultName => "Ellipse";

        public float RadiusX
        {
            get => Width / 2;
            set => Width = value * 2;
        }

        public float RadiusY
        {
            get => Height / 2;
            set => Height = value * 2;
        }

        public float CenterX
        {
            get => Get<float>();
            set => Set(value);
        }

        public float CenterY
        {
            get => Get<float>();
            set => Set(value);
        }

        public override Vector2 Origin
        {
            get => new Vector2(CenterX - RadiusX, CenterY - RadiusY);
            set => (CenterX, CenterY) = (value.X + RadiusX, value.Y + RadiusY);
        }

        public override Geometry GetGeometry(ICacheManager cache)
        {
            return new EllipseGeometry(
                cache.ArtView.Direct2DFactory,
                new SharpDX.Direct2D1.Ellipse(
                    new Vector2(CenterX, CenterY),
                    RadiusX,
                    RadiusY));
        }

        public override RectangleF GetBounds(ICacheManager cache)
        {
            return new RectangleF(CenterX - RadiusX, CenterY - RadiusY, Width, Height);
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

        public float X
        {
            get => Get<float>();
            set => Set(value);
        }

        public float Y
        {
            get => Get<float>();
            set => Set(value);
        }

        public override Vector2 Origin
        {
            get => new Vector2(X, Y);
            set => (X, Y) = (value.X, value.Y);
        }

        public override RectangleF GetBounds(ICacheManager cache)
        {
            return new RectangleF(X, Y, Width, Height);
        }

        protected override string ElementName => "rect";

        public override Geometry GetGeometry(ICacheManager cache)
        {
            return new RectangleGeometry(
                cache.ArtView.Direct2DFactory,
                new RectangleF(X, Y, Width, Height));
        }
    }

    public abstract class Shape : Layer, IGeometricLayer
    {
        protected Shape()
        {
            StrokeInfo = new StrokeInfo();
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

        #region IGeometricLayer Members

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

        public abstract Geometry GetGeometry(ICacheManager factory);

        public override T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe)
        {
            if (!(this is T)) return null;

            var pt = Matrix3x2.TransformPoint(Matrix3x2.Invert(AbsoluteTransform), point);

            var bounds = cache.GetBounds(this);

            if (!bounds.Contains(pt)) return null;

            var geometry = cache.GetGeometry(this);

            if (FillBrush != null &&
                geometry.FillContainsPoint(
                    pt,
                    Matrix3x2.Identity,
                    geometry.FlatteningTolerance))
                return this as T;

            if (StrokeBrush != null &&
                geometry.StrokeContainsPoint(
                    pt,
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
                if (dc != null)
                    dc.DrawGeometryRealization(cache.GetGeometryRealization(this), cache.GetFill(this));
                else
                    target.FillGeometry(cache.GetGeometry(this), cache.GetFill(this));

            if (StrokeBrush != null)
            {
                var stroke = cache.GetStroke(this);

                lock (stroke)
                {
                    target.DrawGeometry(
                        cache.GetGeometry(this),
                        stroke.Brush,
                        stroke.Width,
                        stroke.Style);
                }
            }

            target.Transform = Matrix3x2.Invert(Transform) * target.Transform;
        }

        public override string DefaultName => "Shape";

        public BrushInfo FillBrush
        {
            get => Get<BrushInfo>();
            set => Set(value);
        }

        public BrushInfo StrokeBrush
        {
            get => Get<BrushInfo>();
            set => Set(value);
        }

        public StrokeInfo StrokeInfo
        {
            get => Get<StrokeInfo>();
            set => Set(value);
        }

        #endregion
    }
}