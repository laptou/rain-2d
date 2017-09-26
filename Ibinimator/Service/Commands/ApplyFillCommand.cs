using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.View.Control;

namespace Ibinimator.Service.Commands
{
    public sealed class ApplyFillCommand : LayerCommandBase<IFilledLayer>
    {
        public ApplyFillCommand(long id, IFilledLayer[] targets,
            BrushInfo @new, BrushInfo[] old) : base(id, targets)
        {
            OldFills = old.Select(o => (BrushInfo) o?.Clone()).ToArray();
            NewFill = (BrushInfo) @new?.Clone();
        }

        public override string Description => $"Filled {Targets.Length} layer(s)";

        public BrushInfo NewFill { get; }
        public BrushInfo[] OldFills { get; }

        public override void Do(ArtView artView)
        {
            foreach (var target in Targets)
                lock (target)
                {
                    target.FillBrush = NewFill;
                }
        }

        public override void Undo(ArtView artView)
        {
            for (var i = 0; i < Targets.Length; i++)
                lock (Targets[i])
                {
                    Targets[i].FillBrush = OldFills[i];
                }
        }
    }
}