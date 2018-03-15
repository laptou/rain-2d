using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model.Geometry;
using Rain.Core.Model.Paint;
using Rain.Core.Utility;

namespace Rain.Core.Model.DocumentGraph
{
    public abstract class Shape : Layer, IGeometricLayer
    {
        private bool _suppressed;
        protected Shape() { Stroke = new PenInfo(); }

        public FillRule FillMode
        {
            get => Get<FillRule>();
            set
            {
                Set(value);

                RaiseGeometryChanged();
            }
        }

        /// <inheritdoc />
        public override void RestoreNotifications()
        {
            _suppressed = false;

            base.RestoreNotifications();
        }

        /// <inheritdoc />
        public override void SuppressNotifications()
        {
            _suppressed = true;

            base.SuppressNotifications();
        }

        protected void RaiseFillChanged()
        {
            if (_suppressed) return;

            FillChanged?.Invoke(this, null);
        }

        protected void RaiseGeometryChanged()
        {
            if (_suppressed) return;

            GeometryChanged?.Invoke(this, null);
            RaiseBoundsChanged();
        }

        protected void RaiseStrokeChanged()
        {
            if (_suppressed) return;

            StrokeChanged?.Invoke(this, null);
        }

        #region IGeometricLayer Members

        public event EventHandler FillChanged;
        public event EventHandler GeometryChanged;
        public event EventHandler StrokeChanged;

        public abstract IGeometry GetGeometry(IArtContext factory);

        public override T HitTest<T>(ICacheManager cache, Vector2 point, int minimumDepth)
        {
            if (!(this is T t)) return default;
            if (minimumDepth > 0) return default;

            var pt = Vector2.Transform(point, MathUtils.Invert(AbsoluteTransform));

            var bounds = cache.GetBounds(this);

            if (!bounds.Contains(pt)) return default;

            var geometry = cache.GetGeometry(this);

            if (Fill != null &&
                geometry.FillContains(pt.X, pt.Y))
                return t;

            if (Stroke != null &&
                geometry.StrokeContains(pt.X, pt.Y, Stroke.Width))
                return t;

            return default;
        }

        public override void Render(IRenderContext target, ICacheManager cache, IViewManager view)
        {
            if (!Visible) return;

            // grabbing the value here avoids jitter if the transform is changed during the rendering
            var transform = Transform;

            target.Transform(transform);
            
            if (Fill != null)
                target.FillGeometry(cache.GetGeometry(this), cache.GetFill(this));

            if (Stroke?.Brush != null)
            {
                var pen = cache.GetStroke(this);
                target.DrawGeometry(cache.GetGeometry(this), pen, pen.Width * view.Zoom);
            }

            target.Transform(MathUtils.Invert(transform));
        }

        public override string DefaultName => "Shape";

        public IBrushInfo Fill
        {
            get => Get<IBrushInfo>();
            set
            {
                Fill?.RemoveReference();
                Set(value);
                Fill?.AddReference();
                RaiseFillChanged();
            }
        }

        public IPenInfo Stroke
        {
            get => Get<IPenInfo>();
            set
            {
                Stroke?.RemoveReference();
                Set(value);
                Stroke?.AddReference();
                RaiseStrokeChanged();
            }
        }

        #endregion
    }
}