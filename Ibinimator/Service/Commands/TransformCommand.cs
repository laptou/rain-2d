using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer.Model;
using Ibinimator.Utility;
using Ibinimator.View.Control;

namespace Ibinimator.Service.Commands
{
    public sealed class TransformCommand : LayerCommandBase<ILayer>
    {
        public TransformCommand(long id, ILayer[] targets, Matrix3x2 matrix) : base(id, targets)
        {
            Transform = matrix;
        }

        public override string Description => $"Transformed {Targets.Length} layer(s)";

        public Matrix3x2 Transform { get; }

        public override void Do(IArtContext artView)
        {
            foreach (var layer in Targets)
                lock (layer)
                    layer.ApplyTransform(Transform);
        }

        public override void Undo(IArtContext artView)
        {
            foreach (var layer in Targets)
                lock (layer)
                    layer.ApplyTransform(MathUtils.Invert(Transform));

        }
    }
}