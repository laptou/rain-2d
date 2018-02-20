using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Measurement;
using Rain.Core.Model.Text;
using Rain.Formatter.Svg.Enums;
using Rain.Formatter.Svg.Structure;

namespace Rain.Formatter.Svg.Shapes
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
}