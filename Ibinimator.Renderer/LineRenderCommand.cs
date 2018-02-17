using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Core.Model.Paint;

namespace Ibinimator.Renderer
{
    internal class LineRenderCommand : GeometricRenderCommand
    {
        public LineRenderCommand(Vector2 v1, Vector2 v2, IPen pen) : base(
            false,
            null,
            pen)
        {
            V1 = v1;
            V2 = v2;
        }

        public Vector2 V1 { get; }
        public Vector2 V2 { get; }
    }
}