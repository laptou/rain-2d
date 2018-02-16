using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Service.Commands
{
    public class GroupCommand : LayerCommandBase<ILayer>
    {
        private readonly Dictionary<ILayer, int> _zIndices = new Dictionary<ILayer, int>();
        private          Group                   _group;

        public GroupCommand(long id, ILayer[] targets) : base(id, targets)
        {
            if (targets.Length == 0)
                throw new ArgumentException("Must group at least one layer.");

            Description = $"Grouped {targets.Length} layer(s)";
        }

        public override string Description { get; }

        public override void Do(IArtContext artView)
        {
            var parents = Targets.Select(target => target.Parent).Distinct().ToArray();

            if (parents.Length != 1)
                throw new InvalidOperationException("Layers must all have the same parent.");

            _group = new Group();

            foreach (var target in Targets)
            {
                _zIndices.Add(target, target.Parent.SubLayers.IndexOf(target as Layer));

                target.Parent.Remove(target as Layer);
                _group.Add(target as Layer);
            }

            parents[0].Add(_group);
            _group.Selected = true;
        }

        public override IOperationCommand Merge(IOperationCommand newCommand)
        {
            throw new InvalidOperationException("This operation is cannot be merged.");
        }

        public override void Undo(IArtContext artView)
        {
            if (_group == null) return;

            foreach (var target in Targets)
            {
                _group.Remove(target as Layer);
                _group.Parent.Add(target as Layer, _zIndices[target]);
            }

            _group.Parent.Remove(_group);
            _group = null;
            _zIndices.Clear();
        }
    }
}