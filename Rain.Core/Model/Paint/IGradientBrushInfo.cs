using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Utility;

namespace Rain.Core.Model.Paint
{
    public interface IGradientBrushInfo : IBrushInfo
    {
        Vector2 EndPoint { get; set; }
        Vector2 FocusOffset { get; set; }
        GradientSpace Space { get; set; }
        SpreadMethod SpreadMethod { get; set; }
        Vector2 StartPoint { get; set; }
        ObservableList<GradientStop> Stops { get; set; }
        GradientBrushType Type { get; set; }
    }
}