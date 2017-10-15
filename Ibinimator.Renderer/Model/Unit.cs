using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public enum Unit
    {
        None,

        // angle
        Degrees,
        Radians,

        // length
        Pixels,
        Points,
        Centimeters,
        Inches,
        Millimeters,

        // time
        Milliseconds,
        Frames,
        Seconds,
        Minutes,
        Hours
    }
}