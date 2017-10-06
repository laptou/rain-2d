﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    public interface ILinearGradientBrush : IBrush
    {
        float EndX { get; set; }
        float EndY { get; set; }
        float StartX { get; set; }
        float StartY { get; set; }
    }
}