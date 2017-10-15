using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    internal class RectangleRenderCommand : GeometricRenderCommand
    {
        public RectangleRenderCommand(float left, float top, float width, float height, bool fill, IBrush brush, IPen pen) : base(fill, brush, pen)
        {
            Top = top;
            Left = left;
            Height = height;
            Width = width;
        }

        public float Width { get; }
        public float Left { get; }
        public float Height { get; }
        public float Top { get; }
    }
}