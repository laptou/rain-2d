using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Utility;

namespace Ibinimator.Renderer.Model
{
    public abstract class Layer : Resource, ILayer
    {
        protected Layer()
        {
            Opacity = 1;
            Scale = Vector2.One;
            UpdateTransform();
        }

        /// <summary>
        ///     Returns the entire layer graph starting at this layer,
        ///     as a list.
        /// </summary>
        /// <returns>
        ///     The entire layer graph starting at this layer,
        ///     as a list.
        /// </returns>
        public virtual IEnumerable<Layer> Flatten()
        {
            yield return this;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        protected virtual void UpdateTransform()
        {
            Transform =
                Matrix3x2.CreateTranslation(-Origin) *
                Matrix3x2.CreateScale(Scale) *
                Matrix3x2.CreateSkew(0, Shear) *
                Matrix3x2.CreateRotation(Rotation) *
                Matrix3x2.CreateTranslation(Origin) *
                Matrix3x2.CreateTranslation(Position);
        }

        #region ILayer Members

        public virtual Layer Find(Guid id)
        {
            return id == Id ? this : null;
        }

        public virtual RectangleF GetBounds(ICacheManager cache)
        {
            return new RectangleF(0, 0, Width, Height);
        }

        public virtual IDisposable GetResource(ICacheManager cache, int id)
        {
            return null;
        }

        public abstract T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe) where T : Layer;

        public Layer Hit(ICacheManager cache, Vector2 point, bool includeMe)
        {
            return Hit<Layer>(cache, point, includeMe);
        }

        public abstract void Render(RenderContext target, ICacheManager cache);

        protected void RaiseBoundsChanged()
        {
            BoundsChanged?.Invoke(this, null);
        }
        public event EventHandler BoundsChanged;

        public virtual string DefaultName => "Layer";

        public virtual float Height
        {
            get => Get<float>();
            set => Set(value);
        }

        public virtual Vector2 Scale
        {
            get => Get<Vector2>();
            set
            {
                Set(value);
                UpdateTransform();
            }
        }

        public virtual float Width
        {
            get => Get<float>();
            set => Set(value);
        }

        public virtual void ApplyTransform(Matrix3x2 transform)
        {
            var layerTransform =
                AbsoluteTransform
                * transform
                * MathUtils.Invert(WorldTransform);
            var delta = layerTransform.Decompose();

            Scale = delta.scale;
            Rotation = delta.rotation;
            Position = Vector2.Transform(Origin, layerTransform) - Origin;
            Shear = delta.skew;
        }

        public virtual Vector2 Origin
        {
            get => Get<Vector2>();
            set
            {
                Set(value);
                UpdateTransform();
            }
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

        public Group Parent
        {
            get => Get<Group>();
            protected internal set => Set(value);
        }

        public Vector2 Position
        {
            get => Get<Vector2>();
            set
            {
                Set(value);
                UpdateTransform();
            }
        }

        public float Rotation
        {
            get => Get<float>();
            set
            {
                Set(value);
                UpdateTransform();
            }
        }

        public bool Selected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public float Shear
        {
            get => Get<float>();
            set
            {
                Set(value);
                UpdateTransform();
            }
        }

        public Matrix3x2 Transform
        {
            get => Get<Matrix3x2>();
            protected set => Set(value);
        }

        public Matrix3x2 WorldTransform => Parent?.AbsoluteTransform ?? Matrix3x2.Identity;

        #endregion
    }
}