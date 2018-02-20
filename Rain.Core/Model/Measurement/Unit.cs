using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Measurement
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