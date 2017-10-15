using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Ibinimator.Utility;
using SharpDX.Direct2D1;
using Layer = Ibinimator.Renderer.Model.Layer;

namespace Ibinimator.Renderer.Model
{
    public abstract class Shape : Layer, IGeometricLayer
    {
        protected Shape()
        {
            Stroke = new PenInfo();
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
            FillChanged?.Invoke(this, null);
        }

        protected void RaiseGeometryChanged()
        {
            GeometryChanged?.Invoke(this, null);
        }

        protected void RaiseStrokeChanged()
        {
            StrokeChanged?.Invoke(this, null);
        }

        #region IGeometricLayer Members

        public event EventHandler FillChanged;
        public event EventHandler GeometryChanged;
        public event EventHandler StrokeChanged;

        public abstract IGeometry GetGeometry(ICacheManager factory);

        public override T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe)
        {
            if (!(this is T)) return null;

            var pt = Vector2.Transform(point, MathUtils.Invert(AbsoluteTransform));

            var bounds = cache.GetBounds(this);

            if (!bounds.Contains(pt)) return null;

            var geometry = cache.GetGeometry(this);

            if (Fill != null && geometry.FillContains(pt.X, pt.Y))
                return this as T;

            if (Stroke != null && geometry.StrokeContains(pt.X, pt.Y, Stroke.Width))
                return this as T;

            return null;
        }

        public override void Render(RenderContext target, ICacheManager cache)
        {
            target.Transform(Transform);

            lock (this)
            {
                if (Fill != null)
                    target.FillGeometry(cache.GetGeometry(this), cache.GetFill(this));

                if (Stroke != null)
                    target.DrawGeometry(cache.GetGeometry(this), cache.GetStroke(this));
            }

            target.Transform(MathUtils.Invert(Transform));
        }

        public override string DefaultName => "Shape";

        public BrushInfo Fill
        {
            get => Get<BrushInfo>();
            set
            {
                Set(value);

                RaiseFillBrushChanged();
            }
        }

        public PenInfo Stroke
        {
            get => Get<PenInfo>();
            set
            {
                Set(value);

                RaiseStrokeChanged();
            }
        }

        #endregion
    }
}