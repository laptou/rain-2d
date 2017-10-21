using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Service.Commands
{
    public class ConvertToPathCommand : LayerCommandBase<IGeometricLayer>
    {
        private Path[] _products;
        private IContainerLayer[] _targetParents;

        public ConvertToPathCommand(long id, IGeometricLayer[] targets) : base(id, targets)
        {
            Description = $"Converted {targets.Length} layer(s) to paths";
        }

        public override string Description { get; }

        public IReadOnlyList<Path> Products => _products;

        public override void Do(IArtContext artContext)
        {
            _targetParents = Targets.Select(t => t.Parent).ToArray<IContainerLayer>();

            _products = Targets.Select(t =>
            {
                var path = new Path();
                var geometry = artContext.CacheManager.GetGeometry(t);
                path.Instructions.AddItems(geometry.Read());
                path.Fill = t.Fill;
                path.Stroke = t.Stroke;

                (path.Scale, path.Rotation, path.Shear, path.Position, path.Origin) =
                    (t.Scale, t.Rotation, t.Shear, t.Position, t.Origin);

                return path;
            }).ToArray();

            for (var i = 0; i < _products.Length; i++)
            {
                var index = _targetParents[i].SubLayers.IndexOf(Targets[i] as Layer);
                _targetParents[i].Remove(Targets[i] as Layer);
                _targetParents[i].Add(_products[i], index);
            }
        }

        public override void Undo(IArtContext artContext)
        {
            for (var i = 0; i < _products.Length; i++)
            {
                _targetParents[i].Add(Targets[i] as Layer);
                _targetParents[i].Remove(_products[i]);
            }
        }
    }
}