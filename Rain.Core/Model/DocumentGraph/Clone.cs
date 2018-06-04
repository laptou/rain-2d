using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model.Paint;
using Rain.Core.Utility;

namespace Rain.Core.Model.DocumentGraph
{
    public interface ICloneLayer : ILayer
    {
        bool Override { get; set; }
        ILayer Target { get; set; }
    }

    public class Clone : Layer, ICloneLayer, IFilledLayer, IStrokedLayer
    {
        private bool _suppressed;

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

        protected void RaiseStrokeChanged()
        {
            if (_suppressed) return;

            StrokeChanged?.Invoke(this, null);
        }

        private void OnTargetBoundsChanged(object sender, EventArgs e) { RaiseBoundsChanged(); }

        private void OnTargetChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Target != null)
            {
                Target.BoundsChanged += OnTargetBoundsChanged;
                Target.PropertyChanged += OnTargetPropertyChanged;
            }
        }

        private void OnTargetChanging(object sender, PropertyChangingEventArgs e)
        {
            if (Target != null)
            {
                Target.BoundsChanged -= OnTargetBoundsChanged;
                Target.PropertyChanged -= OnTargetPropertyChanged;
            }
        }

        private void OnTargetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Name))
                RaisePropertyChanged(nameof(DefaultName));

            if (e.PropertyName == nameof(Transform))
                RaiseBoundsChanged();
        }

        #region ICloneLayer Members

        /// <inheritdoc />
        public override RectangleF GetBounds(IArtContext ctx) { return ctx.CacheManager.GetRelativeBounds(Target); }

        /// <inheritdoc />
        public override T HitTest<T>(ICacheManager cache, Vector2 point, int minimumDepth)
        {
            point = Vector2.Transform(point, MathUtils.Invert(AbsoluteTransform));

            var hit = Target.HitTest<T>(cache, point, minimumDepth);

            if (Equals(hit, Target) &&
                this is T l)
                return l;

            return hit;
        }

        /// <inheritdoc />
        public override void Render(IRenderContext target, ICacheManager cache, IViewManager view)
        {
            target.Transform(Transform);

            cache.SuppressInvalidation();

            IBrushInfo fill = null;
            IPenInfo stroke = null;

            // scope the variables to avoid naming conflicts
            {
                if (Target is IFilled filled)
                {
                    fill = filled.Fill;

                    if (Fill != null &&
                        (Override || fill == null))
                        filled.Fill = Fill;
                }

                if (Target is IStroked stroked)
                {
                    stroke = stroked.Stroke;

                    if (Stroke != null &&
                        (Override || stroke == null))
                        stroked.Stroke = Stroke;
                }
            }

            cache.RestoreInvalidation();

            Target.Render(target, cache, view);

            cache.SuppressInvalidation();

            {
                if (Target is IFilled filled)
                    filled.Fill = fill;

                if (Target is IStroked stroked)
                    stroked.Stroke = stroke;
            }

            cache.RestoreInvalidation();

            target.Transform(MathUtils.Invert(Transform));
        }

        /// <inheritdoc />
        public override string DefaultName => $"Clone of {Target.Name ?? Target.DefaultName}";

        public bool Override
        {
            get => Get<bool>();
            set => Set(value);
        }

        public ILayer Target
        {
            get => Get<ILayer>();
            set => Set(value, OnTargetChanging, OnTargetChanged);
        }

        #endregion

        #region IFilledLayer Members

        /// <inheritdoc />
        public event EventHandler FillChanged;

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

        #endregion

        #region IStrokedLayer Members

        /// <inheritdoc />
        public event EventHandler StrokeChanged;

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