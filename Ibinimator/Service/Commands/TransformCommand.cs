using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.Utility;
using Ibinimator.View.Control;
using SharpDX;

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

        public override void Do(ArtView artView)
        {
            foreach (var layer in Targets)
                lock (layer)
                {
                    var layerTransform =
                        layer.AbsoluteTransform
                        * Transform
                        * Matrix3x2.Invert(layer.WorldTransform);
                    var delta = layerTransform.Decompose();

                    layer.Scale = delta.scale;
                    layer.Rotation = delta.rotation;
                    layer.Position = delta.translation;
                    layer.Shear = delta.skew;
                }
        }

        public override void Undo(ArtView artView)
        {
            foreach (var layer in Targets)
                lock (layer)
                {
                    var layerTransform =
                        layer.AbsoluteTransform
                        * Matrix3x2.Invert(Transform)
                        * Matrix3x2.Invert(layer.WorldTransform);
                    var delta = layerTransform.Decompose();


                    layer.Scale = delta.scale;
                    layer.Rotation = delta.rotation;
                    layer.Position = delta.translation;
                    layer.Shear = delta.skew;
                }
        }
    }
}