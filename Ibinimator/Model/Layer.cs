using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.Generic;
using Ibinimator.Shared;
using Ibinimator.View.Control;

namespace Ibinimator.Model
{
    public class Group : Layer
    {

        #region Properties

        public override String DefaultName => "Group";

        #endregion Properties

        #region Methods

        public override IEnumerable<Layer> Flatten()
        {
            yield return this;
        }

        public override RectangleF GetBounds()
        {
            RectangleF first = SubLayers.FirstOrDefault()?.GetTransformedBounds() ?? RectangleF.Empty;

            float x1 = first.Left, y1 = first.Top, x2 = first.Right, y2 = first.Bottom;

            Parallel.ForEach(SubLayers.Skip(1), layer =>
            {
                var bounds = layer.GetTransformedBounds();

                if (bounds.Left < x1) x1 = bounds.Left;
                if (bounds.Top < y1) y1 = bounds.Top;
                if (bounds.Right > x2) x2 = bounds.Right;
                if (bounds.Bottom > y2) y2 = bounds.Bottom;
            });

            return new RectangleF(x1, y1, x2 - x1, y2 - y1);

        }

        #endregion Methods

    }

    public class Layer : Model
    {
        #region Constructors

        public Layer()
        {
            Opacity = 1;
            Scale = Vector2.One;

            SubLayers.CollectionChanged += OnCollectionChanged;
        }

        #endregion Constructors

        #region Properties

        public Matrix3x2 AbsoluteTransform => Transform * WorldTransform;

        public Matrix3x2 WorldTransform => (Parent?.AbsoluteTransform ?? Matrix.Identity);

        public virtual String DefaultName => "Layer";

        public virtual float Height { get => Get<float>(); set => Set(value); }

        public Guid ID { get; } = Guid.NewGuid();

        public Layer Mask { get => Get<Layer>(); set => Set(value); }

        public string Name { get => Get<string>(); set => Set(value); }

        public float Opacity { get => Get<float>(); set => Set(value); }

        public Layer Parent { get => Get<Layer>(); set => Set(value); }

        public Vector2 Position { get => Get<Vector2>(); set { Set(value); RaisePropertyChanged(nameof(Transform)); } }

        public float Rotation { get => Get<float>(); set { Set(value); RaisePropertyChanged(nameof(Transform)); } }

        public bool Selected { get => Get<bool>(); set => Set(value); }

        public ObservableCollection<Layer> SubLayers { get; } = new ObservableCollection<Layer>();

        public Matrix3x2 Transform => 
            Matrix3x2.Scaling(Scale) * 
            Matrix3x2.Rotation(Rotation) * 
            Matrix3x2.Translation(Position);

        public Vector2 Scale { get => Get<Vector2>(); set { Set(value); RaisePropertyChanged(nameof(Transform)); } }

        public virtual float Width { get => Get<float>(); set => Set(value); }

        public float X { get => Position.X; set => Position = new Vector2(value, Y); }

        public float Y { get => Position.Y; set => Position = new Vector2(X, value); }

        #endregion Properties

        #region Methods

        public void Add(Layer child)
        {
            child.Parent = this;
            SubLayers.Add(child);
        }

        /// <summary>
        /// Returns the entire layer graph starting at this layer,
        /// as a list.
        /// </summary>
        /// <returns>
        /// The entire layer graph starting at this layer,
        /// as a list.
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
                var bounds = layer.GetTransformedBounds();

                if (bounds.Left < x1) x1 = bounds.Left;
                if (bounds.Top < y1) y1 = bounds.Top;
                if (bounds.Right > x2) x2 = bounds.Right;
                if (bounds.Bottom > y2) y2 = bounds.Bottom;
            });

            return new RectangleF(x1, y1, x2 - x1, y2 - y1);
        }

        public RectangleF GetTransformedBounds()
        {
            var r = MathUtils.Bounds(GetBounds(), AbsoluteTransform);

            return r;
        }

        public RectangleF GetUnrotatedBounds()
        {
            var r = MathUtils.Bounds(GetBounds(), Matrix3x2.Scaling(Scale) * Matrix3x2.Translation(Position) * WorldTransform);

            return r;
        }

        public virtual Layer Hit(Factory factory, Vector2 point, Matrix3x2 world)
        {
            world *= Transform;

            foreach (var layer in SubLayers)
            {
                var result = layer.Hit(factory, point, world);
                if (result != null) return result;
            }

            return null;
        }

        public void Remove(Layer child)
        {
            child.Parent = null;
            SubLayers.Remove(child);
        }

        public virtual void Render(RenderTarget target, CacheHelper helper)
        {
            foreach (var layer in SubLayers)
            {
                layer.Render(target, helper);
            }
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (Layer layer in e.NewItems)
                layer.PropertyChanged += OnSubLayerChanged;

            if (e.Action == NotifyCollectionChangedAction.Remove)
                foreach (Layer layer in e.OldItems)
                    layer.PropertyChanged -= OnSubLayerChanged;
        }

        private void OnSubLayerChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(sender, e);
        }

        #endregion Methods
    }
}