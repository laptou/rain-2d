using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core;

namespace Ibinimator.Renderer
{
    internal class GeometryRenderCommand : GeometricRenderCommand
    {
        public GeometryRenderCommand(IGeometry geometry, bool fill, IBrush brush, IPen pen) :
            base(fill, brush, pen)
        {
            Geometry = geometry;
        }

        public IGeometry Geometry { get; }
    }
}