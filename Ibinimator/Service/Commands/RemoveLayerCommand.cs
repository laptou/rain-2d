using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Service.Commands
{
    public sealed class RemoveLayerCommand : LayerCommandBase<IContainerLayer>
    {
        private int _index;

        public RemoveLayerCommand(long id, IContainerLayer target, ILayer layer) : base(id, new[] {target})
        {
            Layer = layer;
        }

        public override string Description => $"Removed {Layer.DefaultName}";

        public ILayer Layer { get; }

        public override void Do(IArtContext artView)
        {
            _index = Targets[0].SubLayers.IndexOf(Layer as Layer);
            Targets[0].Remove(Layer as Layer);
        }

        public override void Undo(IArtContext artView)
        {
            Targets[0].Add(Layer as Layer, _index);
        }
    }
}