using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Utility;

namespace Ibinimator.Renderer.Model
{
    public class Group : Layer, IContainerLayer
    {
        private void OnSubLayerChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(sender, e);
        }

        private void OnSubLayerChanging(object sender, PropertyChangingEventArgs e)
        {
            RaisePropertyChanging(sender, e);
        }

        #region IContainerLayer Members

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
            if (SubLayers.Count == 0) return RectangleF.Empty;

            if (cache != null)
                return SubLayers
                    .Select(cache.GetRelativeBounds)
                    .Aggregate(RectangleF.Union);

            return SubLayers
                .Select(l => MathUtils.Bounds(l.GetBounds(null), l.Transform))
                .Aggregate((r1, r2) => RectangleF.Union(r1, r2));
        }

        public override T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe)
        {
            T hit = null;

            foreach (var layer in SubLayers)
            {
                var result = layer.Hit<T>(cache, point, includeMe);
                if (result == null) continue;

                hit = result;
                break;
            }

            if (includeMe && hit != null)
                return this is T ? this as T : hit;

            return hit;
        }

        public void Remove(Layer child)
        {
            if (child.Parent != this)
                throw new InvalidOperationException();

            child.Selected = false;
            child.Parent = null;
            SubLayers.Remove(child);
            child.PropertyChanged -= OnSubLayerChanged;
            child.PropertyChanging -= OnSubLayerChanging;
            LayerRemoved?.Invoke(this, child);
        }

        public override void Render(RenderContext target, ICacheManager cache)
        {
            lock (this)
            {
                target.Transform(Transform);

                foreach (var layer in SubLayers.Reverse())
                    layer.Render(target, cache);

                target.Transform(MathUtils.Invert(Transform));
            }
        }

        public override string DefaultName => "Group";

        public ObservableList<Layer> SubLayers { get; } = new ObservableList<Layer>();

        #endregion
    }
}