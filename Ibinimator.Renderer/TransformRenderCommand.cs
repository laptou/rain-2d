using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    internal class TransformRenderCommand : RenderCommand
    {
        public TransformRenderCommand(Matrix3x2 transform, bool absolute)
        {
            Transform = transform;
            Absolute = absolute;
        }

        public Matrix3x2 Transform { get; }
        public bool Absolute { get; }
    }
}