using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    internal class EllipseRenderCommand : GeometricRenderCommand
    {
        public EllipseRenderCommand(
            float cx,
            float cy,
            float rx,
            float ry,
            bool fill,
            IBrush brush,
            IPen pen) : base(fill, brush, pen)
        {
            CenterX = cx;
            CenterY = cy;
            RadiusX = rx;
            RadiusY = ry;
        }

        public float CenterX { get; }
        public float CenterY { get; }
        public float RadiusX { get; }
        public float RadiusY { get; }
    }
}