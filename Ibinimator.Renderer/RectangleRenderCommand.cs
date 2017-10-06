using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    internal class RectangleRenderCommand : GeometricRenderCommand
    {
        public RectangleRenderCommand(float top, float left, float right, float bottom,
            bool fill, IBrush brush, IPen pen) : base(fill, brush, pen)
        {
            Top = top;
            Left = left;
            Right = right;
            Bottom = bottom;
        }

        public float Bottom { get; }
        public float Left { get; }
        public float Right { get; }

        public float Top { get; }
    }
}