using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Ibinimator.Core.Utility;

namespace Ibinimator.Core.Model.DocumentGraph
{
    public abstract class Layer : Model, ILayer
    {
        protected Layer()
        {
            Opacity = 1;
            Visible = true;
            Transform = Matrix3x2.Identity;
        }

        public virtual ILayer Find(Guid id) { return id == Id ? this : null; }

        public override int GetHashCode() { return Id.GetHashCode(); }

        protected void RaiseBoundsChanged() { BoundsChanged?.Invoke(this, null); }

        #region ILayer Members

        public event EventHandler BoundsChanged;

        public virtual void ApplyTransform(
            Matrix3x2? local  = null,
            Matrix3x2? global = null)
        {
            Transform = (local ?? Matrix3x2.Identity) *
                        Transform *
                        WorldTransform *
                        (global ?? Matrix3x2.Identity) *
                        MathUtils.Invert(WorldTransform);
        }

        public virtual IEnumerable<ILayer> Flatten(int depth)
        {
            if (depth >= 0) yield return this;
        }

        /// <summary>
        ///     Returns the entire layer graph starting at this layer,
        ///     as a list.
        /// </summary>
        /// <returns>
        ///     The entire layer graph starting at this layer,
        ///     as a list.
        /// </returns>
        public virtual IEnumerable<ILayer> Flatten() { yield return this; }

        public virtual RectangleF GetBounds(ICacheManager cache)
        {
            return new RectangleF(0, 0, Width, Height);
        }

        public abstract T HitTest<T>(ICacheManager cache, Vector2 point, int minimumDepth)
            where T : ILayer;

        public abstract void Render(RenderContext target, ICacheManager cache, IViewManager view);

        public virtual string DefaultName => "Layer";

        public virtual int Depth => Parent?.Depth + 1 ?? 0;

        public virtual float Height
        {
            get => Get<float>();
            set => Set(value);
        }

        public virtual int Order => Parent?.Order + Parent?.SubLayers.IndexOf(this) + 1 ?? 0;

        /// <inheritdoc />
        public virtual int Size => 1;

        public virtual float Width
        {
            get => Get<float>();
            set => Set(value);
        }

        public Matrix3x2 AbsoluteTransform => Transform * WorldTransform;

        public IGeometricLayer Clip
        {
            get => Get<IGeometricLayer>();
            set => Set(value);
        }

        public Guid Id { get; } = Guid.NewGuid();

        public ILayer Mask
        {
            get => Get<ILayer>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public virtual float Opacity
        {
            get => Get<float>();
            set => Set(value);
        }

        public IContainerLayer Parent
        {
            get => Get<Group>();
            set => Set(value);
        }

        public virtual bool Selected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public virtual Matrix3x2 Transform
        {
            get => Get<Matrix3x2>();
            protected set => Set(value);
        }

        public bool Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public Matrix3x2 WorldTransform =>
            Parent?.AbsoluteTransform ?? Matrix3x2.Identity;

        #endregion
    }
}