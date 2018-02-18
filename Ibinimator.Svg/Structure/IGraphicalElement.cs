using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Ibinimator.Core.Model;
using Ibinimator.Core.Model.Measurement;
using Ibinimator.Core.Model.Paint;
using Ibinimator.Svg.Enums;

namespace Ibinimator.Svg.Structure
{
    public interface IGraphicalElement : IElement
    {
        RectangleF? Clip { get; set; }
        Iri? ClipPath { get; set; }
        FillRule ClipRule { get; set; }
        Color Color { get; set; }
        ColorInterpolation ColorFilterInterpolation { get; set; }
        ColorInterpolation ColorInterpolation { get; set; }
        Cursor Cursor { get; set; }
        Direction Direction { get; set; }
        Iri? Filter { get; set; }
        Length? Kerning { get; set; }
        Length? LetterSpacing { get; set; }
        Iri? Mask { get; set; }
        float Opacity { get; set; }
        Matrix3x2 Transform { get; set; }
    }
}