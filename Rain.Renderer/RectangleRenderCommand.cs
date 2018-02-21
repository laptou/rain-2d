using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Paint;

namespace Rain.Renderer
{
    internal class RectangleRenderCommand : GeometricRenderCommand
    {
        public RectangleRenderCommand(
            float left, float top, float width, float height, bool fill, IBrush brush, IPen pen) :
            base(fill, brush, pen)
        {
            Top = top;
            Left = left;
            Height = height;
            Width = width;
        }

        public float Height { get; }
        public float Left { get; }
        public float Top { get; }

        public float Width { get; }
    }
}