using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg.Reader
{
    public enum AlignmentBaseline
    {
        Inherit = -1,
        Auto = 0,
        Baseline,
        BeforeEdge,
        TextBeforeEdge,
        Middle,
        Central,
        AfterEdge,
        TextAfterEdge,
        Ideographic,
        Alphabetic,
        Hanging,
        Mathematical
    }
}