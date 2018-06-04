using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Utility;

namespace Rain.Commands
{
    public sealed class TransformCommand : LayerCommandBase<ILayer>, IMergeableOperationCommand
    {
        public TransformCommand(
            long id, IReadOnlyList<ILayer> targets, Matrix3x2? local = null, Matrix3x2? global = null) : base(
            id,
            targets)
        {
            Local = local ?? Matrix3x2.Identity;
            Global = global ?? Matrix3x2.Identity;
        }

        public Matrix3x2 Global { get; }

        public Matrix3x2 Local { get; }

        #region IMergeableOperationCommand Members

        public override void Do(IArtContext artView)
        {
            foreach (var layer in Targets)
                lock (layer)
                {
                    layer.ApplyTransform(Local, Global);
                }
        }

        public IOperationCommand Merge(IOperationCommand newCommand)
        {
            if (!Targets.SequenceEqual(newCommand.Targets)) return null;

            var transformCommand = (TransformCommand) newCommand;

            return new TransformCommand(Id, Targets, Local * transformCommand.Local, Global * transformCommand.Global);
        }

        public override void Undo(IArtContext artView)
        {
            foreach (var layer in Targets)
                lock (layer)
                {
                    layer.ApplyTransform(MathUtils.Invert(Local), MathUtils.Invert(Global));
                }
        }

        public override string Description => $"Transformed {Targets.Count} layer(s)";

        #endregion
    }
}