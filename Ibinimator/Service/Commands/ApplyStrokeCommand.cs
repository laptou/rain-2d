using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Renderer.Model;
using Ibinimator.View.Control;

namespace Ibinimator.Service.Commands
{
    public sealed class ApplyStrokeCommand : LayerCommandBase<IStrokedLayer>
    {
        public ApplyStrokeCommand(long id, IStrokedLayer[] targets,
            PenInfo newPenInfo, IEnumerable<PenInfo> oldPenInfos) : base(id, targets)
        {
            OldStrokes = oldPenInfos.Select(i => i?.Clone<PenInfo>()).ToArray();

            NewStroke = newPenInfo?.Clone<PenInfo>();
        }

        public override string Description => $"Stroked {Targets.Length} layer(s)";

        public PenInfo NewStroke { get; }
        public PenInfo[] OldStrokes { get; }

        public override void Do(IArtContext artView)
        {
            foreach (var target in Targets)
                lock (target)
                    target.Stroke = NewStroke;
        }

        public override void Undo(IArtContext artView)
        {
            for (var i = 0; i < Targets.Length; i++)
                lock (Targets[i])
                    Targets[i].Stroke = OldStrokes[i];
        }
    }
}