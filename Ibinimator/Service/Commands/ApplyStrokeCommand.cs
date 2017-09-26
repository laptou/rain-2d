using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.View.Control;

namespace Ibinimator.Service.Commands
{
    public sealed class ApplyStrokeCommand : LayerCommandBase<IStrokedLayer>
    {
        public ApplyStrokeCommand(long id, IStrokedLayer[] targets,
            BrushInfo newStrokeBrush, BrushInfo[] oldStrokeBrushes,
            StrokeInfo newStrokeInfo, StrokeInfo[] oldStrokeInfos) : base(id, targets)
        {
            OldStrokes =
                oldStrokeBrushes.Zip(oldStrokeInfos,
                    (b, i) => (b?.Clone<BrushInfo>(), i?.Clone<StrokeInfo>())).ToArray();

            NewStroke = (newStrokeBrush?.Clone<BrushInfo>(), newStrokeInfo?.Clone<StrokeInfo>());
        }

        public override string Description => $"Stroked {Targets.Length} layer(s)";

        public (BrushInfo, StrokeInfo) NewStroke { get; }
        public (BrushInfo, StrokeInfo)[] OldStrokes { get; }

        public override void Do(ArtView artView)
        {
            foreach (var target in Targets)
                lock (target)
                {
                    (target.StrokeBrush, target.StrokeInfo) = NewStroke;
                }
        }

        public override void Undo(ArtView artView)
        {
            for (var i = 0; i < Targets.Length; i++)
                lock (Targets[i])
                {
                    (Targets[i].StrokeBrush, Targets[i].StrokeInfo) = OldStrokes[i];
                }
        }
    }
}