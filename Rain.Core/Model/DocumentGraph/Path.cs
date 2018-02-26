using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Geometry;
using Rain.Core.Utility;

namespace Rain.Core.Model.DocumentGraph
{
    public class Path : Shape
    {
        public Path() { Instructions.CollectionChanged += OnNodesChanged; }

        public override string DefaultName => "Path";

        public ObservableList<PathInstruction> Instructions { get; } =
            new ObservableList<PathInstruction>();

        public override RectangleF GetBounds(IArtContext ctx)
        {
            return ctx.CacheManager.GetGeometry(this).Bounds();
        }

        public override IGeometry GetGeometry(IArtContext ctx)
        {
            var pg = ctx.RenderContext.CreateGeometry();

            // ToArray to avoid modification-during-iteration errors
            pg.Load(Instructions.ToArray());

            return pg;
        }

        public void Update() { RaiseGeometryChanged(); }

        private void OnNodesChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            RaiseGeometryChanged();
        }
    }
}