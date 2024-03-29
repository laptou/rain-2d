﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rain.Renderer
{
    internal class TransformRenderCommand : RenderCommand
    {
        public TransformRenderCommand(Matrix3x2 transform, bool absolute)
        {
            Transform = transform;
            Absolute = absolute;
        }

        public bool Absolute { get; }

        public Matrix3x2 Transform { get; }
    }
}