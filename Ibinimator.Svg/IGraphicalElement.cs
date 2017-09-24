using System.Drawing;
using System.Numerics;
using System.Xml.Linq;

namespace Ibinimator.Svg
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