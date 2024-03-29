﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Measurement;
using Rain.Core.Model.Paint;
using Rain.Formatter.Svg.Enums;

namespace Rain.Formatter.Svg.Structure
{
    public interface IGraphicalElement : IElement
    {
        RectangleF? Clip { get; set; }
        Uri ClipPath { get; set; }
        FillRule ClipRule { get; set; }
        Color Color { get; set; }
        ColorInterpolation ColorFilterInterpolation { get; set; }
        ColorInterpolation ColorInterpolation { get; set; }
        Cursor Cursor { get; set; }
        Direction Direction { get; set; }
        Uri Filter { get; set; }
        Length? Kerning { get; set; }
        Length? LetterSpacing { get; set; }
        Uri Mask { get; set; }
        float Opacity { get; set; }
        Matrix3x2 Transform { get; set; }
    }
}