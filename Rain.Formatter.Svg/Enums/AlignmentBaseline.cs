using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Formatter.Svg.Enums
{
    public enum AlignmentBaseline
    {
        Inherit = -1,
        Auto    = 0,
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