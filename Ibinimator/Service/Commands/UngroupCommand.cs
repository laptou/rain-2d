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
    public class UngroupCommand : LayerCommandBase<IContainerLayer>
    {
        private readonly Dictionary<Layer, IContainerLayer> _layers = new Dictionary<Layer, IContainerLayer>();
        private readonly Dictionary<IContainerLayer, Group> _parents = new Dictionary<IContainerLayer, Group>();

        public UngroupCommand(long id, IContainerLayer[] targets) : base(id, targets)
        {
            Description = $"Ungrouped {targets.Length} layer(s)";
        }

        public override void Do(ArtView artView)
        {
            foreach (var target in Targets)
            {
                foreach (var layer in target.SubLayers.ToArray())
                {
                    target.Remove(layer);
                    target.Parent.Add(layer);

                    (layer.Scale, layer.Rotation, layer.Position, layer.Shear)
                        = (layer.Transform * target.Transform).Decompose();

                    _layers.Add(layer, target);
                }

                _parents.Add(target, target.Parent);

                target.Parent.Remove(target as Layer);
            }
        }

        public override void Undo(ArtView artView)
        {
            foreach (var (layer, target) in _layers.AsTuples())
            {
                layer.Parent.Remove(layer);

                (layer.Scale, layer.Rotation, layer.Position, layer.Shear)
                    = (layer.Transform * Matrix3x2.Invert(target.Transform)).Decompose();

                target.Add(layer);
            }

            foreach (var (target, parent) in _parents.AsTuples())
            {
                parent.Add(target as Layer);
            }

            _layers.Clear();
            _parents.Clear();
        }

        public override string Description { get; }
    }
}