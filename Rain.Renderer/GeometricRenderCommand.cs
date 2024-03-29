﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Paint;

namespace Rain.Renderer
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
}