using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Utility;

namespace Ibinimator.Core
{
    public interface IGradientBrush : IBrush
    {
        GradientSpace                Space { get; set; }
        ObservableList<GradientStop> Stops { get; }
    }

    public enum GradientSpace
    {
        Absolute,
        Relative
    }
}