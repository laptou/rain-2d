using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core;
using Rain.Core.Model.DocumentGraph;

namespace Rain.Commands
{
    public class ConvertToPathCommand : LayerCommandBase<IGeometricLayer>
    {
        private Path[]            _products;
        private IContainerLayer[] _targetParents;

        public ConvertToPathCommand(long id, IReadOnlyCollection<IGeometricLayer> targets) : base(
            id,
            targets.Where(l => !(l is Path)).ToArray()) { }

        public override string Description => $"Converted {Targets.Count} layer(s) to paths";

        public IReadOnlyList<Path> Products => _products;

        public override void Do(IArtContext artContext)
        {
            var parents = new List<IContainerLayer>();
            var products = new List<Path>();

            foreach (var target in Targets)
            {
                parents.Add(target.Parent);

                var path = new Path();
                var geometry = artContext.CacheManager.GetFillGeometry(target);

                path.Instructions.AddItems(geometry.Read());

                path.Fill = target.Fill;
                path.Stroke = target.Stroke;
                path.ApplyTransform(target.Transform);

                products.Add(path);

                var index = target.Parent.SubLayers.IndexOf(target);

                target.Parent.Add(path, index);
                target.Parent.Remove(target);

                path.Selected = target.Selected;
            }

            _targetParents = parents.ToArray();
            _products = products.ToArray();
        }

        public override void Undo(IArtContext artContext)
        {
            for (var i = 0; i < _products.Length; i++)
            {
                var index = _targetParents[i].SubLayers.IndexOf(_products[i]);

                _targetParents[i].Add(Targets[i], index);
                _targetParents[i].Remove(_products[i]);
            }
        }
    }
}