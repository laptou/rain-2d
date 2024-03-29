﻿using System;
using System.Collections.Generic;
using System.Linq;

using Rain.Core.Utility;

using System.Threading.Tasks;

using Rain.Core;
using Rain.Core.Model.DocumentGraph;

namespace Rain.Commands
{
    public class UngroupCommand : LayerCommandBase<IContainerLayer>
    {
        private readonly Dictionary<ILayer, IContainerLayer> _layers = new Dictionary<ILayer, IContainerLayer>();

        private readonly Dictionary<IContainerLayer, IContainerLayer> _parents =
            new Dictionary<IContainerLayer, IContainerLayer>();

        public UngroupCommand(long id, IContainerLayer[] targets) : base(id, targets)
        {
            Description = $"Ungrouped {targets.Length} layer(s)";
        }

        public override string Description { get; }

        public override void Do(IArtContext artView)
        {
            foreach (var target in Targets)
            {
                foreach (var layer in target.SubLayers.ToArray())
                {
                    target.Remove(layer);
                    target.Parent.Add(layer);
                    target.Selected = true;

                    layer.ApplyTransform(global: target.Transform);

                    _layers.Add(layer, target);
                }

                _parents.Add(target, target.Parent);

                target.Parent.Remove(target as Layer);
            }
        }

        public override void Undo(IArtContext artView)
        {
            foreach (var (layer, target) in _layers.AsTuples())
            {
                layer.Parent.Remove(layer);

                layer.ApplyTransform(global: MathUtils.Invert(target.Transform));

                target.Add(layer);
            }

            foreach (var (target, parent) in _parents.AsTuples())
                parent.Add(target as Layer);

            _layers.Clear();
            _parents.Clear();
        }
    }
}