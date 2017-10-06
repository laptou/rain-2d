using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    internal class TransformRenderCommand : RenderCommand
    {
        public TransformRenderCommand(Matrix3x2 transform)
        {
            Transform = transform;
        }

        public Matrix3x2 Transform { get; }
    }
}