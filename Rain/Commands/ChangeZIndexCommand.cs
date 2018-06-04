using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Utility;

namespace Rain.Commands
{
    public sealed class ChangeZIndexCommand : LayerCommandBase<ILayer>, IMergeableOperationCommand
    {
        public ChangeZIndexCommand(long id, IReadOnlyList<ILayer> targets, int delta) : base(id, targets)
        {
            Delta = delta;
        }

        public int Delta { get; }

        #region IMergeableOperationCommand Members

        public override void Do(IArtContext artContext)
        {
            foreach (var target in Targets)
            {
                var siblings = target.Parent.SubLayers;
                var index = siblings.IndexOf(target as Layer);
                siblings.Move(index, MathUtils.Clamp(0, siblings.Count - 1, index + Delta));
            }

            artContext.SelectionManager.UpdateBounds();
        }

        public IOperationCommand Merge(IOperationCommand newCommand)
        {
            if (!Targets.SequenceEqual(newCommand.Targets)) return null;

            var changeZIndexCommand = (ChangeZIndexCommand) newCommand;

            return new ChangeZIndexCommand(Id, Targets, Delta + changeZIndexCommand.Delta);
        }

        public override void Undo(IArtContext artContext)
        {
            foreach (var target in Targets)
            {
                var siblings = target.Parent.SubLayers;
                var index = siblings.IndexOf(target as Layer);
                siblings.Move(index, MathUtils.Clamp(0, siblings.Count - 1, index - Delta));
            }

            artContext.SelectionManager.UpdateBounds();
        }

        public override string Description => $"Changed z-index of {Targets.Count} layer(s)";

        #endregion
    }
}