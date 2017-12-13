﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public interface IGradientBrush : IBrush
    {
        IList<GradientStop> Stops { get; }
        GradientSpace Space { get; set; }
    }

    public enum GradientSpace
    {
        Absolute,
        Relative
    }
}