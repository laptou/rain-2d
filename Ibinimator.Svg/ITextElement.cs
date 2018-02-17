using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model;
using Ibinimator.Core.Model.Measurement;
using Ibinimator.Core.Model.Text;
using Ibinimator.Svg.Enums;
using Ibinimator.Svg.Shapes;

namespace Ibinimator.Svg
{
    public interface ITextElement : IShapeElement, IContainerElement<IInlineTextElement>
    {
        AlignmentBaseline AlignmentBaseline { get; set; }
        BaselineShift     BaselineShift     { get; set; }
        string            FontFamily        { get; set; }
        Length?           FontSize          { get; set; }
        FontStretch?      FontStretch       { get; set; }
        FontStyle?        FontStyle         { get; set; }
        FontWeight?       FontWeight        { get; set; }
        string            Text              { get; set; }
    }

    public interface IInlineTextElement : ITextElement
    {
        int Position { get; set; }
    }
}