using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Measurement;
using Ibinimator.Core.Model.Text;
using Ibinimator.Svg.Enums;
using Ibinimator.Svg.Structure;

namespace Ibinimator.Svg.Shapes
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