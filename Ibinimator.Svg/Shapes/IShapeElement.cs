using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model;
using Ibinimator.Core.Model.Measurement;
using Ibinimator.Core.Model.Paint;
using Ibinimator.Svg.Enums;
using Ibinimator.Svg.Paint;

namespace Ibinimator.Svg.Shapes
{
    public interface IShapeElement : IGraphicalElement
    {
        Paint.Paint Fill             { get; set; }
        float    FillOpacity      { get; set; }
        FillRule FillRule         { get; set; }
        Paint.Paint Stroke           { get; set; }
        float[]  StrokeDashArray  { get; set; }
        float    StrokeDashOffset { get; set; }
        LineCap  StrokeLineCap    { get; set; }
        LineJoin StrokeLineJoin   { get; set; }
        float    StrokeMiterLimit { get; set; }
        float    StrokeOpacity    { get; set; }
        Length   StrokeWidth      { get; set; }
    }
}