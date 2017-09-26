using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.View.Control;

namespace Ibinimator.Service.Commands
{
    public class GroupCommand : LayerCommandBase<ILayer>
    {
        private Group _group;
        private readonly Dictionary<ILayer, int> _indices = new Dictionary<ILayer, int>();

        public GroupCommand(long id, ILayer[] targets) : base(id, targets)
        {
            if(targets.Length == 0)
                throw new ArgumentException("Must group at least one layer.");

            Description = $"Grouped {targets.Length} layer(s)";
        }

        public override void Do(ArtView artView)
        {
            var parents = Targets.Select(target => target.Parent).Distinct().ToArray();

            if (parents.Length != 1)
                throw new InvalidOperationException("Layers must all have the same parent.");

            _group = new Group();

            foreach (var target in Targets)
            {
                _indices.Add(target, target.Parent.SubLayers.IndexOf(target as Layer));

                target.Parent.Remove(target as Layer);
                _group.Add(target as Layer);
            }

            parents[0].Add(_group);
        }

        public override void Undo(ArtView artView)
        {
            if (_group == null) return;

            foreach (var target in Targets)
            {
                _group.Remove(target as Layer);
                _group.Parent.Add(target as Layer, _indices[target]);
            }

            _group.Parent.Remove(_group);
            _group = null;
            _indices.Clear();
        }

        public override string Description { get; }
    }
}
