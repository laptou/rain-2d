using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public interface ILinearGradientBrush : IGradientBrush
    {
        float EndX   { get; set; }
        float EndY   { get; set; }
        float StartX { get; set; }
        float StartY { get; set; }
    }
}