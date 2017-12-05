using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Model;

namespace Ibinimator.Renderer {
    internal class ClearRenderCommand : RenderCommand
    {
        public ClearRenderCommand(Color color) { Color = color; }

        public Color Color { get; }
    }
}