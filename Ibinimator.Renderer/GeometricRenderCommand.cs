using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    internal class GeometricRenderCommand : RenderCommand
    {
        public GeometricRenderCommand(bool fill, IBrush brush, IPen pen)
        {
            Fill = fill;
            Brush = brush;
            Pen = pen;
        }

        public IBrush Brush { get; }
        public bool Fill { get; }
        public IPen Pen { get; }
    }

    internal class GeometryRenderCommand : GeometricRenderCommand
    {
        public GeometryRenderCommand(IGeometry geometry, bool fill, IBrush brush, IPen pen) : base(fill, brush, pen)
        {
            Geometry = geometry;
        }

        public IGeometry Geometry { get; }
    }
}