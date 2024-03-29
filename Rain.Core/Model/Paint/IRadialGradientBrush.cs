﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Paint
{
    public interface IRadialGradientBrush : IGradientBrush
    {
        float CenterX { get; set; }
        float CenterY { get; set; }
        float FocusX { get; set; }
        float FocusY { get; set; }
        float RadiusX { get; set; }
        float RadiusY { get; set; }
    }
}