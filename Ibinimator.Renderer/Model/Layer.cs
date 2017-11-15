using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ibinimator.Core.Utility;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public abstract class Layer : Resource, ILayer
    {
        protected Layer()
        {
            Opacity = 1;
            Transform = Matrix3x2.Identity;
        }

        /// <summary>
        ///     Returns the entire layer graph starting at this layer,
        ///     as a list.
        /// </summary>
        /// <returns>
        ///     The entire layer graph starting at this layer,
        ///     as a list.
        /// </returns>
        public virtual IEnumerable<ILayer> Flatten()
        {
            yield return this;
        }

        public override int GetHashCode() { return Id.GetHashCode(); }

        protected void RaiseBoundsChanged() { BoundsChanged?.Invoke(this, null); }

        #region ILayer Members

        public event EventHandler BoundsChanged;

        public virtual void ApplyTransform(
            Matrix3x2? local = null,
            Matrix3x2? global = null)
        {
            Transform = (local ?? Matrix3x2.Identity) *
                        Transform *
                        WorldTransform *
                        (global ?? Matrix3x2.Identity) *
                        MathUtils.Invert(WorldTransform);
        }

        public virtual ILayer Find(Guid id) { return id == Id ? this : null; }

        public virtual RectangleF GetBounds(ICacheManager cache)
        {
            return new RectangleF(0, 0, Width, Height);
        }

        public abstract T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe)
            where T : ILayer;

        public ILayer Hit(ICacheManager cache, Vector2 point, bool includeMe)
        {
            return Hit<Layer>(cache, point, includeMe);
        }

        public abstract void Render(RenderContext target, ICacheManager cache, IViewManager view);

        public virtual string DefaultName => "Layer";

        public virtual float Height
        {
            get => Get<float>();
            set => Set(value);
        }

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

        public float Opacity
        {
            get => Get<float>();
            set => Set(value);
        }

        public IContainerLayer Parent
        {
            get => Get<Group>();
            set => Set(value);
        }

        public bool Selected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public Matrix3x2 Transform
        {
            get => Get<Matrix3x2>();
            protected set => Set(value);
        }

        public Matrix3x2 WorldTransform =>
            Parent?.AbsoluteTransform ?? Matrix3x2.Identity;

        #endregion
    }
}