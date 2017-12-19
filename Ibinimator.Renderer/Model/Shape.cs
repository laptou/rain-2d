using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Core.Utility;

using SharpDX.Direct2D1;

namespace Ibinimator.Renderer.Model
{
    public abstract class Shape : Layer, IGeometricLayer
    {
        protected Shape() { Stroke = new PenInfo(); }

        public FillMode FillMode
        {
            get => Get<FillMode>();
            set
            {
                Set(value);

                RaiseGeometryChanged();
            }
        }

        protected void RaiseFillBrushChanged() { FillChanged?.Invoke(this, null); }

        protected void RaiseGeometryChanged()
        {
            GeometryChanged?.Invoke(this, null);
            RaiseBoundsChanged();
        }

        protected void RaiseStrokeChanged() { StrokeChanged?.Invoke(this, null); }

        #region IGeometricLayer Members

        public event EventHandler FillChanged;
        public event EventHandler GeometryChanged;
        public event EventHandler StrokeChanged;

        public abstract IGeometry GetGeometry(ICacheManager factory);

        public override T HitTest<T>(ICacheManager cache, Vector2 point, int minimumDepth)
        {
            if (!(this is T t)) return default;
            if (minimumDepth > 0) return default;

            var pt = Vector2.Transform(point, MathUtils.Invert(AbsoluteTransform));

            var bounds = cache.GetBounds(this);

            if (!bounds.Contains(pt)) return default;

            var geometry = cache.GetGeometry(this);

            if (Fill != null && geometry.FillContains(pt.X, pt.Y))
                return t;

            if (Stroke != null && geometry.StrokeContains(pt.X, pt.Y, Stroke.Width))
                return t;

            return default;
        }

        public override void Render(RenderContext target, ICacheManager cache, IViewManager view)
        {
            if (!Visible) return;

            target.Transform(Transform);

            lock (this)
            {
                if (Fill != null)
                    target.FillGeometry(cache.GetGeometry(this), cache.GetFill(this));

                if (Stroke?.Brush != null)
                {
                    var pen = cache.GetStroke(this);
                    target.DrawGeometry(cache.GetGeometry(this), pen, pen.Width * view.Zoom);
                }
            }

            target.Transform(MathUtils.Invert(Transform));
        }

        public override string DefaultName => "Shape";

        public IBrushInfo Fill
        {
            get => Get<IBrushInfo>();
            set
            {
                Set(value);
                RaiseFillBrushChanged();
            }
        }

        public IPenInfo Stroke
        {
            get => Get<IPenInfo>();
            set
            {
                Set(value);
                RaiseStrokeChanged();
            }
        }

        #endregion
    }
}