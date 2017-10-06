using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg
{
    public interface ITextElement : IShapeElement, IContainerElement<IInlineTextElement>
    {
        AlignmentBaseline AlignmentBaseline { get; set; }
        BaselineShift BaselineShift { get; set; }
        string FontFamily { get; set; }
        Length? FontSize { get; set; }
        FontStretch? FontStretch { get; set; }
        FontStyle? FontStyle { get; set; }
        FontWeight? FontWeight { get; set; }
        string Text { get; set; }
    }

    public enum FontStyle
    {
        Normal,
        Italic,
        Oblique
    }

    public interface IInlineTextElement : ITextElement
    {
        int Position { get; set; }
    }
}