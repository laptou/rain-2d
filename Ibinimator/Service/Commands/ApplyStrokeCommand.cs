using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core;

namespace Ibinimator.Service.Commands
{
    public sealed class ApplyStrokeCommand : LayerCommandBase<IStrokedLayer>
    {
        public ApplyStrokeCommand(
            long     id, IStrokedLayer[]               targets,
            IPenInfo newPenInfo, IEnumerable<IPenInfo> oldPenInfos) : base(id, targets)
        {
            OldStrokes = oldPenInfos.Select(i => i?.Clone<IPenInfo>()).ToArray();

            NewStroke = newPenInfo?.Clone<IPenInfo>();
        }

        public override string Description => $"Stroked {Targets.Length} layer(s)";

        public IPenInfo   NewStroke  { get; }
        public IPenInfo[] OldStrokes { get; }

        public override void Do(IArtContext artView)
        {
            foreach (var target in Targets)
                lock (target)
                {
                    target.Stroke = NewStroke;
                }
        }

        public override IOperationCommand Merge(IOperationCommand newCommand)
        {
            if (!Targets.SequenceEqual(newCommand.Targets)) return null;

            var applyStrokeCommand = (ApplyStrokeCommand) newCommand;

            return new ApplyStrokeCommand(Id, Targets,
                                          applyStrokeCommand.NewStroke, OldStrokes);
        }

        public override void Undo(IArtContext artView)
        {
            for (var i = 0; i < Targets.Length; i++)
                lock (Targets[i])
                {
                    Targets[i].Stroke = OldStrokes[i];
                }
        }
    }
}