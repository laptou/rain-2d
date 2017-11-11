using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Service.Commands
{
    public sealed class TransformCommand : LayerCommandBase<ILayer>
    {
        public TransformCommand(long id, ILayer[] targets, Matrix3x2 matrix) : base(id, targets)
        {
            Transform = matrix;

            if (float.IsNaN(matrix.M11) ||
                float.IsNaN(matrix.M12) ||
                float.IsNaN(matrix.M21) ||
                float.IsNaN(matrix.M22) ||
                float.IsNaN(matrix.M31) ||
                float.IsNaN(matrix.M32) ||
                Math.Abs(matrix.M11) > 1000 ||
                Math.Abs(matrix.M12) > 1000 ||
                Math.Abs(matrix.M21) > 1000 ||
                Math.Abs(matrix.M22) > 1000 ||
                Math.Abs(matrix.M31) > 1000 ||
                Math.Abs(matrix.M32) > 1000)
                Debugger.Break();
        }

        public override string Description => $"Transformed {Targets.Length} layer(s)";

        public Matrix3x2 Transform { get; }

        public override void Do(IArtContext artView)
        {
            foreach (var layer in Targets)
            {
                lock (layer)
                {
                    layer.ApplyTransform(Transform);

                    if (float.IsNaN(layer.Transform.M11) ||
                        float.IsNaN(layer.Transform.M12) ||
                        float.IsNaN(layer.Transform.M21) ||
                        float.IsNaN(layer.Transform.M22) ||
                        float.IsNaN(layer.Transform.M31) ||
                        float.IsNaN(layer.Transform.M32) ||
                        Math.Abs(layer.Transform.M11) > 1000000 ||
                        Math.Abs(layer.Transform.M12) > 1000000 ||
                        Math.Abs(layer.Transform.M21) > 1000000 ||
                        Math.Abs(layer.Transform.M22) > 1000000 ||
                        Math.Abs(layer.Transform.M31) > 1000000 ||
                        Math.Abs(layer.Transform.M32) > 1000000)
                        Debugger.Break();
                }
            }

            // point of the transform: take matrix A that applies transform in absolute space
            // apply to matrix B such that given matrix W representing world transform:
            // WC = AWB
        }

        public override void Undo(IArtContext artView)
        {
            foreach (var layer in Targets)
            {
                lock (layer)
                {
                    layer.ApplyTransform(MathUtils.Invert(Transform));
                }
            }
        }
    }
}