using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Paint;
using Ibinimator.Core.Utility;

namespace Ibinimator.Core.Model.DocumentGraph
{
    public interface ICloneLayer : ILayer
    {
        ILayer Target { get; set; }
    }

    public class Clone : Layer, ICloneLayer, IFilledLayer, IStrokedLayer
    {
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
            if (e.PropertyName == nameof(Transform))
                RaiseBoundsChanged();
        }

        #region ICloneLayer Members

        /// <inheritdoc />
        public override RectangleF GetBounds(ICacheManager cache)
        {
            return cache.GetRelativeBounds(Target);
        }

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
        public override void Render(RenderContext target, ICacheManager cache, IViewManager view)
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

                    if (Fill != null)
                        filled.Fill = Fill;
                }

                if (Target is IStroked stroked)
                {
                    stroke = stroked.Stroke;

                    if (Stroke != null)
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

        public ILayer Target
        {
            get => Get<ILayer>();
            set => Set(value, OnTargetChanging, OnTargetChanged);
        }

        #endregion

        #region IFilledLayer Members

        /// <inheritdoc />
        public event EventHandler FillChanged;

        /// <inheritdoc />
        public IBrushInfo Fill { get; set; }

        #endregion

        #region IStrokedLayer Members

        /// <inheritdoc />
        public event EventHandler StrokeChanged;

        /// <inheritdoc />
        public IPenInfo Stroke { get; set; }

        #endregion
    }
}