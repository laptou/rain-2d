using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Utility;

namespace Rain.Core.Model.Paint
{
    public interface IGradientBrush : IBrush
    {
        GradientSpace Space { get; set; }
        ObservableList<GradientStop> Stops { get; }
    }

    public enum GradientSpace
    {
        Absolute,
        Relative
    }
}