using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;

namespace Ibinimator.Service.Commands
{
    public sealed class ApplyFillCommand : LayerCommandBase<IFilledLayer>
    {
        public ApplyFillCommand(long id, IFilledLayer[] targets,
            IBrushInfo @new, IBrushInfo[] old) : base(id, targets)
        {
            OldFills = old.Select(o => (IBrushInfo) o?.Clone()).ToArray();
            NewFill = (IBrushInfo) @new?.Clone();
        }

        public override IOperationCommand Merge(IOperationCommand newCommand)
        {
            if (!Targets.SequenceEqual(newCommand.Targets)) return null;

            var applyFillCommand = (ApplyFillCommand) newCommand;

            return new ApplyFillCommand(Id, Targets, applyFillCommand.NewFill, OldFills);
        }

        public override string Description => $"Filled {Targets.Length} layer(s)";

        public IBrushInfo NewFill { get; }
        public IBrushInfo[] OldFills { get; }

        public override void Do(IArtContext artView)
        {
            foreach (var target in Targets)
                lock (target)
                {
                    target.Fill = NewFill;
                }
        }

        public override void Undo(IArtContext artView)
        {
            for (var i = 0; i < Targets.Length; i++)
                lock (Targets[i])
                {
                    Targets[i].Fill = OldFills[i];
                }
        }
    }
}