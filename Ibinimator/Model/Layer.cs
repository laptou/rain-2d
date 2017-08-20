using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Ibinimator.Service;
using Ibinimator.Shared;
using SharpDX;
using SharpDX.Direct2D1;

namespace Ibinimator.Model
{
    public class Group : Layer
    {
        public override string DefaultName => "Group";

        public override T Hit<T>(Factory factory, Vector2 point, Matrix3x2 world)
        {
            var hit = base.Hit<T>(factory, point, world);
            if (hit != null)
                if (this is T)
                    return Selected ? hit : this as T;
                else return hit;
            return null;
        }

        public override IEnumerable<Layer> Flatten()
        {
            yield return this;
        }

        public override RectangleF GetBounds()
        {
            switch (SubLayers.Count)
            {
                case 0:
                    return RectangleF.Empty;
                case 1:
                    return SubLayers[0].GetAbsoluteBounds();
                default:
                    var first = SubLayers[0].GetRelativeBounds();

                    float x1 = first.Left, y1 = first.Top, x2 = first.Right, y2 = first.Bottom;

                    Parallel.ForEach(SubLayers.Skip(1), layer =>
                    {
                        var bounds = layer.GetRelativeBounds();

                        if (bounds.Left < x1) x1 = bounds.Left;
                        if (bounds.Top < y1) y1 = bounds.Top;
                        if (bounds.Right > x2) x2 = bounds.Right;
                        if (bounds.Bottom > y2) y2 = bounds.Bottom;
                    });

                    return new RectangleF(x1, y1, x2 - x1, y2 - y1);
            }
        }
    }

    [XmlInclude(typeof(Group))]
    [XmlInclude(typeof(Shape))]
    public class Layer : Model
    {
        public Layer()
        {
            Opacity = 1;
            Scale = Vector2.One;
            UpdateTransform();
        }
        
        [XmlIgnore]
        public virtual string DefaultName => "Layer";

        [XmlAttribute]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [XmlElement]
        public Layer Mask
        {
            get => Get<Layer>();
            set => Set(value);
        }

        [Undoable]
        [XmlAttribute]
        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        [Undoable]
        [Animatable]
        [XmlAttribute]
        public float Opacity
        {
            get => Get<float>();
            set => Set(value);
        }

        [Undoable]
        [XmlIgnore]
        public Layer Parent
        {
            get => Get<Layer>();
            private set => Set(value);
        }

        [XmlAttribute]
        public bool Selected
        {
            get => Get<bool>();
            set => Set(value);
        }

        [XmlElement("Layer")]
        public ObservableCollection<Layer> SubLayers { get; set; } = new ObservableCollection<Layer>();

        [XmlIgnore]
        public Matrix3x2 AbsoluteTransform => Transform * WorldTransform;

        [XmlIgnore]
        public Matrix3x2 WorldTransform => Parent?.AbsoluteTransform ?? Matrix.Identity;

        [XmlIgnore]
        public Matrix3x2 Transform
        {
            get => Get<Matrix3x2>();
            private set => Set(value);
        }

        [Undoable]
        [Animatable]
        [XmlIgnore]
        public Vector2 Scale
        {
            get => Get<Vector2>();
            set
            {
                Set(value);
                UpdateTransform();
            }
        }

        [XmlAttribute]
        public float ScaleX
        {
            get => Scale.X;
            set => Scale = new Vector2(value, Scale.Y);
        }

        [XmlAttribute]
        public float ScaleY
        {
            get => Scale.Y;
            set => Scale = new Vector2(Scale.X, value);
        }

        [Undoable]
        [Animatable]
        [XmlAttribute]
        public float Shear
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
        [XmlAttribute]
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
        [XmlIgnore]
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
        [XmlAttribute]
        public virtual float Height
        {
            get => Get<float>();
            set => Set(value);
        }

        [Undoable]
        [Animatable]
        [XmlAttribute]
        public virtual float Width
        {
            get => Get<float>();
            set => Set(value);
        }

        [XmlAttribute]
        public float X
        {
            get => Position.X;
            set => Position = new Vector2(value, Y);
        }

        [XmlAttribute]
        public float Y
        {
            get => Position.Y;
            set => Position = new Vector2(X, value);
        }

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

        private void OnSubLayerChanging(object sender, PropertyChangingEventArgs e)
        {
            RaisePropertyChanging(sender, e);
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

        public Layer Find(Guid id)
        {
            return
                id == Id
                    ? this
                    : SubLayers
                        .Select(layer => layer.Find(id))
                        .FirstOrDefault(l => l != null);
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

            foreach (var layer in SubLayers)
            {
                var graph = layer.Flatten();

                foreach (var child in graph)
                    yield return child;
            }
        }

        public virtual RectangleF GetBounds()
        {
            float x1 = 0, y1 = 0, x2 = Width, y2 = Height;

            Parallel.ForEach(SubLayers, layer =>
            {
                var bounds = layer.GetAbsoluteBounds();

                if (bounds.Left < x1) x1 = bounds.Left;
                if (bounds.Top < y1) y1 = bounds.Top;
                if (bounds.Right > x2) x2 = bounds.Right;
                if (bounds.Bottom > y2) y2 = bounds.Bottom;
            });

            return new RectangleF(x1, y1, x2 - x1, y2 - y1);
        }

        public RectangleF GetAbsoluteBounds()
        {
            var r = MathUtils.Bounds(GetBounds(), AbsoluteTransform);

            return r;
        }

        public RectangleF GetAxisAlignedBounds()
        {
            var decomp = AbsoluteTransform.Decompose();

            var r = MathUtils.Bounds(GetBounds(),
                Matrix3x2.Scaling(decomp.scale) * Matrix3x2.Translation(decomp.translation));

            return r;
        }

        public RectangleF GetRelativeBounds()
        {
            var r = MathUtils.Bounds(GetBounds(), Transform);

            return r;
        }

        public virtual T Hit<T>(Factory factory, Vector2 point, Matrix3x2 world) where T : Layer
        {
            world *= Transform;

            foreach (var layer in SubLayers)
            {
                var result = layer.Hit<T>(factory, point, world);
                if (result != null) return result;
            }

            return null;
        }

        public Layer Hit(Factory factory, Vector2 point, Matrix3x2 world)
        {
            return Hit<Layer>(factory, point, world);
        }

        public virtual void Render(RenderTarget target, ICacheManager helper)
        {
            lock (this)
            {
                foreach (var layer in SubLayers.Reverse())
                    layer.Render(target, helper);
            }
        }

        private void UpdateTransform()
        {
            Transform =
                Matrix3x2.Scaling(Scale) *
                Matrix3x2.Skew(0, Shear) *
                Matrix3x2.Rotation(Rotation) *
                Matrix3x2.Translation(Position);
        }

        private void OnSubLayerChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(sender, e);
        }
    }
}