using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Paint;

namespace Rain.Commands
{
    public sealed class ApplyFillCommand : LayerCommandBase<IFilledLayer>, IMergeableOperationCommand
    {
        public ApplyFillCommand(
            long id, IReadOnlyList<IFilledLayer> targets, IBrushInfo @new, IBrushInfo[] old) : base(id, targets)
        {
            OldFills = old.Select(o => o?.Clone<IBrushInfo>()).ToArray();
            NewFill = @new?.Clone<IBrushInfo>();
        }

        public IBrushInfo NewFill { get; }
        public IBrushInfo[] OldFills { get; }

        #region IMergeableOperationCommand Members

        public override void Do(IArtContext artView)
        {
            foreach (var target in Targets)
                lock (target)
                {
                    target.Fill = NewFill;
                }
        }

        public IOperationCommand Merge(IOperationCommand newCommand)
        {
            if (!Targets.SequenceEqual(newCommand.Targets)) return null;

            var applyFillCommand = (ApplyFillCommand) newCommand;

            return new ApplyFillCommand(Id, Targets, applyFillCommand.NewFill, OldFills);
        }

        public override void Undo(IArtContext artView)
        {
            for (var i = 0; i < Targets.Count; i++)
                lock (Targets[i])
                {
                    Targets[i].Fill = OldFills[i];
                }
        }

        public override string Description => $"Filled {Targets.Count} layer(s)";

        #endregion
    }
}