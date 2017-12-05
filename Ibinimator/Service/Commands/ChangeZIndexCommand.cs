using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Service.Commands
{
    public sealed class ChangeZIndexCommand : LayerCommandBase<ILayer>
    {
        public ChangeZIndexCommand(long id, ILayer[] targets, int delta) : base(id, targets)
        {
            Delta = delta;
        }

        public int Delta { get; }

        public override IOperationCommand Merge(IOperationCommand newCommand)
        {
            if (!Targets.SequenceEqual(newCommand.Targets)) return null;

            var changeZIndexCommand = (ChangeZIndexCommand) newCommand;

            return new ChangeZIndexCommand(Id, Targets, Delta + changeZIndexCommand.Delta);
        }
        public override string Description => $"Changed z-index of {Targets.Length} layer(s)";

        public override void Do(IArtContext artContext)
        {
            foreach (var target in Targets)
            {
                var siblings = target.Parent.SubLayers;
                var index = siblings.IndexOf(target as Layer);
                siblings.Move(index, MathUtils.Clamp(0, siblings.Count - 1, index + Delta));
            }

            artContext.SelectionManager.Update(true);
        }

        public override void Undo(IArtContext artContext)
        {
            foreach (var target in Targets)
            {
                var siblings = target.Parent.SubLayers;
                var index = siblings.IndexOf(target as Layer);
                siblings.Move(index, MathUtils.Clamp(0, siblings.Count - 1, index - Delta));
            }

            artContext.SelectionManager.Update(true);
        }
    }
}