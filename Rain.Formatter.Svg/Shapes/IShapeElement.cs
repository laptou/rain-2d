using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Measurement;
using Rain.Core.Model.Paint;
using Rain.Formatter.Svg.Structure;

namespace Rain.Formatter.Svg.Shapes
{
    public interface IShapeElement : IGraphicalElement
    {
        Paint.Paint Fill { get; set; }
        float FillOpacity { get; set; }
        FillRule FillRule { get; set; }
        Paint.Paint Stroke { get; set; }
        float[] StrokeDashArray { get; set; }
        float StrokeDashOffset { get; set; }
        LineCap StrokeLineCap { get; set; }
        LineJoin StrokeLineJoin { get; set; }
        float StrokeMiterLimit { get; set; }
        float StrokeOpacity { get; set; }
        Length StrokeWidth { get; set; }
    }
}