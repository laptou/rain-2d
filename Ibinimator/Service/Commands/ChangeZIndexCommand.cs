using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Renderer.Model;
using Ibinimator.Utility;
using Ibinimator.View.Control;

namespace Ibinimator.Service.Commands
{
    public sealed class ChangeZIndexCommand : LayerCommandBase<ILayer>
    {
        public ChangeZIndexCommand(long id, ILayer[] targets, int delta) : base(id, targets)
        {
            Delta = delta;
        }

        public int Delta { get; }

        public override string Description => $"Changed z-index of {Targets.Length} layer(s)";

        public override void Do(IArtContext artView)
        {
            foreach (var target in Targets)
            {
                var siblings = target.Parent.SubLayers;
                var index = siblings.IndexOf(target as Layer);
                siblings.Move(index, MathUtils.Clamp(0, siblings.Count - 1, index + Delta));
            }
        }

        public override void Undo(IArtContext artView)
        {
            foreach (var target in Targets)
            {
                var siblings = target.Parent.SubLayers;
                var index = siblings.IndexOf(target as Layer);
                siblings.Move(index, MathUtils.Clamp(0, siblings.Count - 1, index - Delta));
            }
        }
    }
}