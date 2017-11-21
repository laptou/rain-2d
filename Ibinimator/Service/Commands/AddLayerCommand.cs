using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Service.Commands
{
    public sealed class AddLayerCommand : LayerCommandBase<IContainerLayer>
    {
        public AddLayerCommand(long id, IContainerLayer target, ILayer layer) : base(id, new[] {target})
        {
            Layer = layer;
        }

        public override IOperationCommand Merge(IOperationCommand newCommand)
        {
            throw new InvalidOperationException("This operation is not stackable.");
        }

        public override string Description => $"Added {Layer.DefaultName}";

        public ILayer Layer { get; }

        public override void Do(IArtContext artView) { Targets[0].Add(Layer as Layer); }

        public override void Undo(IArtContext artView) { Targets[0].Remove(Layer as Layer); }
    }
}