﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Utility;

namespace Rain.Core.Model.DocumentGraph
{
    public class Group : Layer, IContainerLayer
    {
        public override ILayer Find(Guid id)
        {
            if (id == Id) return this;

            var subLayer = SubLayers.FirstOrDefault(layer => layer.Id == id);

            if (subLayer != null) return subLayer;

            return SubLayers.OfType<Group>().Select(layer => layer.Find(id)).FirstOrDefault(l => l != null);
        }

        private void OnBoundsChanged(object sender, EventArgs e) { RaiseBoundsChanged(); }

        private void OnSubLayerChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(sender, e);

            if (e.PropertyName == nameof(ILayer.Transform))
                RaiseBoundsChanged();
        }

        private void OnSubLayerChanging(object sender, PropertyChangingEventArgs e)
        {
            RaisePropertyChanging(sender, e);
        }

        #region IContainerLayer Members

        public event EventHandler<ILayer> LayerAdded;

        public event EventHandler<ILayer> LayerRemoved;

        /// <inheritdoc />
        public ILayer this[Guid id] => Find(id);

        /// <inheritdoc />
        public ILayer this[int index] => SubLayers[index];

        public void Add(ILayer child, int index = -1)
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
            child.BoundsChanged += OnBoundsChanged;
            LayerAdded?.Invoke(this, child);
        }

        public override IEnumerable<ILayer> Flatten(int depth)
        {
            if (depth < 0) yield break;

            yield return this;

            foreach (var layer in SubLayers)
            {
                var graph = layer.Flatten(depth - 1);

                foreach (var child in graph)
                    yield return child;
            }
        }

        public override IEnumerable<ILayer> Flatten()
        {
            yield return this;

            foreach (var layer in SubLayers)
            {
                var graph = layer.Flatten();

                foreach (var child in graph)
                    yield return child;
            }
        }

        public override RectangleF GetBounds(IArtContext ctx)
        {
            if (SubLayers.Count == 0) return RectangleF.Empty;

            if (ctx != null)
                return SubLayers.Select(ctx.CacheManager.GetRelativeBounds).Aggregate(RectangleF.Union);

            return SubLayers.Select(l => MathUtils.Bounds(l.GetBounds(null), l.Transform))
                            .Aggregate((r1, r2) => RectangleF.Union(r1, r2));
        }

        public override T HitTest<T>(ICacheManager cache, Vector2 point, int minimumDepth)
        {
            var hit = default(T);

            foreach (var layer in SubLayers)
            {
                var result = layer.HitTest<T>(cache, point, minimumDepth - 1);

                if (result == null) continue;

                hit = result;

                break;
            }

            if (minimumDepth <= 0 &&
                hit != null)
                return this is T t ? t : hit;

            return hit;
        }

        public void Remove(ILayer child)
        {
            if (child.Parent != this)
                throw new InvalidOperationException();

            child.Selected = false;
            child.Parent = null;
            SubLayers.Remove(child);
            child.PropertyChanged -= OnSubLayerChanged;
            child.PropertyChanging -= OnSubLayerChanging;
            child.BoundsChanged -= OnBoundsChanged;

            LayerRemoved?.Invoke(this, child);
        }

        public override void Render(IRenderContext target, ICacheManager cache, IViewManager view)
        {
            if (!Visible) return;

            lock (this)
            {
                // grabbing the value here avoids jitter if the transform is changed during the rendering
                var transform = Transform;

                target.Transform(transform);

                foreach (var layer in SubLayers.Reverse())
                    layer.Render(target, cache, view);

                target.Transform(MathUtils.Invert(transform));
            }
        }

        public override string DefaultName => "Group";

        /// <inheritdoc cref="ILayer.Size" />
        public override int Size => SubLayers.Count;

        public ObservableList<ILayer> SubLayers { get; } = new ObservableList<ILayer>();

        #endregion
    }
}