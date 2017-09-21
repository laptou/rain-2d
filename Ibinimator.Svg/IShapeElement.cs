using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg
{
    public interface IShapeElement : IElement
    {
        Paint? Fill { get; set; }
        float FillOpacity { get; set; }
        FillRule FillRule { get; set; }
        Paint? Stroke { get; set; }
        float[] StrokeDashArray { get; set; }
        float StrokeDashOffset { get; set; }
        float StrokeOpacity { get; set; }
        Length StrokeWidth { get; set; }
    }
}