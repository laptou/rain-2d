using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.Generic;

namespace Ibinimator.Model
{
    public class Group : Layer
    {
        public override String DefaultName => "Group";

        public override float X { get => GetBounds().X; }

        public override float Y { get => GetBounds().Y; }

        public override RectangleF GetBounds()
        {
            RectangleF start = SubLayers.FirstOrDefault()?.GetBounds() ?? RectangleF.Empty;

            float x1 = start.X, y1 = start.Y, x2 = start.Right, y2 = start.Bottom;

            Parallel.ForEach(SubLayers.Skip(1), layer =>
            {
                var bounds = layer.GetBounds();

                if (bounds.Left < x1) x1 = bounds.Left;
                if (bounds.Top < y1) y1 = bounds.Top;
                if (bounds.Right > x2) x2 = bounds.Right;
                if (bounds.Bottom > y2) y2 = bounds.Bottom;
            });

            return new RectangleF(x1, y1, x2 - x1, y2 - y1);
        }

        public override IEnumerable<Layer> Flatten()
        {
            yield return this;
        }
    }

    public class Layer : Model
    {
        public Layer()
        {
            Opacity = 1;

            SubLayers.CollectionChanged += OnCollectionChanged;
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

        #region Properties

        public Layer Mask { get => Get<Layer>(); set => Set(value); }

        public string Name { get => Get<string>(); set => Set(value); }

        public virtual String DefaultName => "Layer";

        public Guid ID { get; } = Guid.NewGuid();

        public float Opacity { get => Get<float>(); set => Set(value); }

        public ObservableCollection<Layer> SubLayers { get; } = new ObservableCollection<Layer>();

        public virtual float X { get => Get<float>(); set => Set(value); }

        public virtual float Y { get => Get<float>(); set => Set(value); }

        public virtual float Width { get => GetBounds().Width; set { } }

        public virtual float Height { get => GetBounds().Height; set { } }

        public bool Selected { get => Get<bool>(); set => Set(value); }

        public Layer Parent { get => Get<Layer>(); set => Set(value); }

        #endregion Properties

        #region Methods

        public virtual RectangleF GetBounds()
        {
            float x1 = X, y1 = Y, x2 = X, y2 = Y;

            Parallel.ForEach(SubLayers, layer =>
            {
                var bounds = layer.GetBounds();

                if (bounds.Left < x1) x1 = bounds.Left;
                if (bounds.Top < y1) y1 = bounds.Top;
                if (bounds.Right > x2) x2 = bounds.Right;
                if (bounds.Bottom > y2) y2 = bounds.Bottom;
            });

            return new RectangleF(x1, y1, x2, y2);
        }

        public virtual void Transform(Matrix3x2 mat)
        {
            X = X * mat.ScaleVector.X + mat.TranslationVector.X;
            Y = Y * mat.ScaleVector.Y + mat.TranslationVector.Y;
            foreach (var l in SubLayers) l.Transform(mat);
        }

        public virtual Layer Hit(Factory factory, Vector2 point)
        {
            foreach (var layer in SubLayers)
            {
                var result = layer.Hit(factory, point);
                if (result != null) return result;
            }

            return null;
        }

        public void Add(Layer child)
        {
            child.Parent = this;
            SubLayers.Add(child);
        }

        public void Remove(Layer child)
        {
            child.Parent = null;
            SubLayers.Remove(child);
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

        #endregion Methods
    }
}