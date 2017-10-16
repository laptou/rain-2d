using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;
using Vector2 = System.Numerics.Vector2;

namespace Ibinimator.Renderer.Model
{
    public class Path : Shape
    {
        public Path()
        {
            Instructions.CollectionChanged += OnNodesChanged;
        }

        private void OnNodesChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            RaiseGeometryChanged();
        }

        public override string DefaultName => "Path";

        public ObservableList<PathInstruction> Instructions { get; } = new ObservableList<PathInstruction>();

        public override RectangleF GetBounds(ICacheManager cache)
        {
            return cache.GetGeometry(this).Bounds();
        }

        public override IGeometry GetGeometry(ICacheManager cache)
        {
            var pg = cache.Context.RenderContext.CreateGeometry();
            pg.Load(Instructions);
            return pg;
        }

        public void Update()
        {
            RaiseGeometryChanged();
        }

        private void NodeOnPropertyChanged(object o, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            RaiseGeometryChanged();
        }
    }
}