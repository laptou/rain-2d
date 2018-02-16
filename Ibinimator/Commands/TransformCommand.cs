using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Core.Utility;

namespace Ibinimator.Service.Commands
{
    public sealed class TransformCommand : LayerCommandBase<ILayer>
    {
        public TransformCommand(
            long id, ILayer[] targets, Matrix3x2? local = null, Matrix3x2? global = null) : base(
            id,
            targets)
        {
            Local = local ?? Matrix3x2.Identity;
            Global = global ?? Matrix3x2.Identity;
        }

        public override string Description => $"Transformed {Targets.Length} layer(s)";
        public Matrix3x2 Global { get; }

        public Matrix3x2 Local { get; }

        public override void Do(IArtContext artView)
        {
            foreach (var layer in Targets)
                lock (layer)
                {
                    layer.ApplyTransform(Local, Global);
                }
        }

        public override IOperationCommand Merge(IOperationCommand newCommand)
        {
            if (!Targets.SequenceEqual(newCommand.Targets)) return null;

            var transformCommand = (TransformCommand) newCommand;

            return new TransformCommand(Id,
                                        Targets,
                                        Local * transformCommand.Local,
                                        Global * transformCommand.Global);
        }

        public override void Undo(IArtContext artView)
        {
            foreach (var layer in Targets)
                lock (layer)
                {
                    layer.ApplyTransform(MathUtils.Invert(Local), MathUtils.Invert(Global));
                }
        }
    }
}