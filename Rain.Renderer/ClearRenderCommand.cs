using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model;

namespace Rain.Renderer
{
    internal class ClearRenderCommand : RenderCommand
    {
        public ClearRenderCommand(Color color) { Color = color; }

        public Color Color { get; }
    }
}