using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Linq;
using System.Xml.Serialization;
using Ibinimator.Service;
using Ibinimator.Shared;
using SharpDX;
using SharpDX.Direct2D1;

namespace Ibinimator.Model
{
    public class Group : Layer, IContainerLayer
    {
        public override string DefaultName => "Group";

        public ObservableList<Layer> SubLayers { get; } = new ObservableList<Layer>();

        protected override string ElementName => "g";

        public event EventHandler<Layer> LayerAdded;

        public event EventHandler<Layer> LayerRemoved;

        public void Add(Layer child, int index = -1)
        {
            if (child.Parent != null)
                throw new InvalidOperationException();

            child.Parent = this;

            if (index == -1)
                SubLayers.Add(child);
            else
                SubLayers.Insert(index, child);

            child.PropertyChanged += OnSubLayerChanged;
            child.PropertyChanging += OnSubLayerChanging;
            LayerAdded?.Invoke(this, child);
        }

        public override Layer Find(Guid id)
        {
            if (id == Id) return this;

            var subLayer = SubLayers.FirstOrDefault(layer => layer.Id == id);

            if (subLayer != null) return subLayer;

            return
                SubLayers
                    .OfType<Group>()
                    .Select(layer => layer.Find(id))
                    .FirstOrDefault(l => l != null);
        }

        public override IEnumerable<Layer> Flatten()
        {
            yield return this;

            foreach (var layer in SubLayers)
            {
                var graph = layer.Flatten();

                foreach (var child in graph)
                    yield return child;
            }
        }

        public override RectangleF GetBounds(ICacheManager cache)
        {
            throw new InvalidOperationException();
        }

        public override XElement GetElement()
        {
            var element = base.GetElement();

            foreach (var layer in SubLayers.Reverse())
                element.Add(layer.GetElement());

            return element;
        }

        public override T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe)
        {
            T hit = null;

            point = Matrix3x2.TransformPoint(Matrix3x2.Invert(Transform), point);

            foreach (var layer in SubLayers)
            {
                var result = layer.Hit<T>(cache, point, includeMe);
                if (result != null) hit = result;
            }

            if (includeMe && hit != null)
                return this is T ? this as T : hit;

            return hit;
        }

        public void Remove(Layer child)
        {
            if (child.Parent != this)
                throw new InvalidOperationException();

            child.Parent = null;
            SubLayers.Remove(child);
            child.PropertyChanged -= OnSubLayerChanged;
            child.PropertyChanging -= OnSubLayerChanging;
            LayerRemoved?.Invoke(this, child);
        }

        public override void Render(RenderTarget target, ICacheManager cache)
        {
            lock (this)
            {
                target.Transform = Transform * target.Transform;

                foreach (var layer in SubLayers.Reverse())
                    layer.Render(target, cache);

                target.Transform = Matrix3x2.Invert(Transform) * target.Transform;
            }
        }

        private void OnSubLayerChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(sender, e);
        }

        private void OnSubLayerChanging(object sender, PropertyChangingEventArgs e)
        {
            RaisePropertyChanging(sender, e);
        }
    }
    
    public abstract class Layer : Resource, ILayer
    {
        protected Layer()
        {
            Opacity = 1;
            Scale = Vector2.One;
            UpdateTransform();
        }

        public Matrix3x2 AbsoluteTransform => Transform * WorldTransform;

        public virtual string DefaultName => "Layer";

        [Undoable]
        [Animatable]
        public virtual float Height
        {
            get => Get<float>();
            set => Set(value);
        }

        public Guid Id { get; } = Guid.NewGuid();

        public ILayer Mask
        {
            get => Get<ILayer>();
            set => Set(value);
        }

        public IGeometricLayer Clip
        {
            get => Get<IGeometricLayer>();
            set => Set(value);
        }

        [Undoable]
        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        [Undoable]
        [Animatable]
        public float Opacity
        {
            get => Get<float>();
            set => Set(value);
        }

        [Undoable]
        public Group Parent
        {
            get => Get<Group>();
            protected internal set => Set(value);
        }

        [Undoable]
        [Animatable]
        public Vector2 Position
        {
            get => Get<Vector2>();
            set
            {
                Set(value);
                UpdateTransform();
            }
        }

        [Undoable]
        [Animatable]
        public float Rotation
        {
            get => Get<float>();
            set
            {
                Set(value);
                UpdateTransform();
            }
        }

        [Undoable]
        [Animatable]
        public virtual Vector2 Scale
        {
            get => Get<Vector2>();
            set
            {
                Set(value);
                UpdateTransform();
            }
        }

        public float ScaleX
        {
            get => Scale.X;
            set => Scale = new Vector2(value, Scale.Y);
        }

        public float ScaleY
        {
            get => Scale.Y;
            set => Scale = new Vector2(Scale.X, value);
        }

        public bool Selected
        {
            get => Get<bool>();
            set => Set(value);
        }

        [Undoable]
        [Animatable]
        public float Shear
        {
            get => Get<float>();
            set
            {
                Set(value);
                UpdateTransform();
            }
        }

        [XmlIgnore]
        public Matrix3x2 Transform
        {
            get => Get<Matrix3x2>();
            private set => Set(value);
        }

        [Undoable]
        [Animatable]
        public virtual float Width
        {
            get => Get<float>();
            set => Set(value);
        }

        public Matrix3x2 WorldTransform => Parent?.AbsoluteTransform ?? Matrix.Identity;

        public float X
        {
            get => Position.X;
            set => Position = new Vector2(value, Y);
        }

        public float Y
        {
            get => Position.Y;
            set => Position = new Vector2(X, value);
        }

        protected abstract string ElementName { get; }

        public abstract T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe) where T : Layer;

        public abstract void Render(RenderTarget target, ICacheManager cache);

        public virtual IDisposable GetResource(ICacheManager cache, int id)
        {
            return null;
        }

        public virtual Layer Find(Guid id)
        {
            return id == Id ? this : null;
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

        public virtual RectangleF GetBounds(ICacheManager cache)
        {
            return new RectangleF(0, 0, Width, Height);
        }

        public override XElement GetElement()
        {
            var element = new XElement(XNamespace.Get("http://www.w3.org/2000/svg") + ElementName);

            if (Name != null)
                element.Add(new XAttribute("id", Name));

            element.Add(new XAttribute("opacity", Opacity));

            // extract transform w/o scale
            // in SVG, transform is also applied to stroke
            // which is unnacceptable
            element.Add(new XAttribute("transform", Transform.ToCss()));

            return element;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public Layer Hit(ICacheManager cache, Vector2 point, bool includeMe)
        {
            return Hit<Layer>(cache, point, includeMe);
        }

        private void UpdateTransform()
        {
            Transform =
                Matrix3x2.Scaling(Scale) *
                Matrix3x2.Skew(0, Shear) *
                Matrix3x2.Rotation(Rotation) *
                Matrix3x2.Translation(Position);
        }
    }
}