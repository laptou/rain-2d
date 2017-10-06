using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public override Vector2 Origin
        {
            get => new Vector2(CenterX - RadiusX, CenterY - RadiusY);
            set => (CenterX, CenterY) = (value.X + RadiusX, value.Y + RadiusY);
        }

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

        public override RectangleF GetBounds(ICacheManager cache)
        {
            return new RectangleF(CenterX - RadiusX, CenterY - RadiusY, Width, Height);
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
                RaiseGeometryChanged();
            }
        }

        public override Vector2 Origin
        {
            get => new Vector2(X, Y);
            set => (X, Y) = (value.X, value.Y);
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

        public override RectangleF GetBounds(ICacheManager cache)
        {
            return new RectangleF(X, Y, Width, Height);
        }

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

                RaiseGeometryChanged();
            }
        }

        protected void RaiseFillBrushChanged()
        {
            FillBrushChanged?.Invoke(this, null);
        }

        protected void RaiseGeometryChanged()
        {
            GeometryChanged?.Invoke(this, null);
        }

        protected void RaiseStrokeBrushChanged()
        {
            StrokeBrushChanged?.Invoke(this, null);
        }

        protected void RaiseStrokeInfoChanged()
        {
            StrokeInfoChanged?.Invoke(this, null);
        }

        #region IGeometricLayer Members

        public event EventHandler FillBrushChanged;
        public event EventHandler GeometryChanged;
        public event EventHandler StrokeBrushChanged;
        public event EventHandler StrokeInfoChanged;

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
                lock (this)
                {
                    if (dc != null)
                        dc.DrawGeometryRealization(cache.GetGeometryRealization(this), cache.GetFill(this));
                    else
                        target.FillGeometry(cache.GetGeometry(this), cache.GetFill(this));
                }

            if (StrokeBrush != null)
                lock (this)
                {
                    var stroke = cache.GetStroke(this);

                    target.DrawGeometry(
                        cache.GetGeometry(this),
                        stroke.Brush,
                        stroke.Width,
                        stroke.Style);
                }

            target.Transform = Matrix3x2.Invert(Transform) * target.Transform;
        }

        public override string DefaultName => "Shape";

        public BrushInfo FillBrush
        {
            get => Get<BrushInfo>();
            set
            {
                Set(value);

                RaiseFillBrushChanged();
            }
        }

        public BrushInfo StrokeBrush
        {
            get => Get<BrushInfo>();
            set
            {
                Set(value);

                RaiseStrokeBrushChanged();
            }
        }

        public StrokeInfo StrokeInfo
        {
            get => Get<StrokeInfo>();
            set
            {
                Set(value);

                RaiseStrokeInfoChanged();
            }
        }

        #endregion
    }
}