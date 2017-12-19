using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Utility;

namespace Ibinimator.Core
{
    public interface IGradientBrush : IBrush
    {
        ObservableList<GradientStop> Stops { get; }
        GradientSpace Space { get; set; }
    }

    public enum GradientSpace
    {
        Absolute,
        Relative
    }
}