using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg
{
    public interface ITextElement : IElement
    {
        AlignmentBaseline AlignmentBaseline { get; set; }
        BaselineShift BaselineShift { get; set; }
        string FontFamily { get; set; }
        Length FontSize { get; set; }
        FontStretch FontStretch { get; set; }
        FontWeight FontWeight { get; set; }
        string Text { get; set; }
    }
}