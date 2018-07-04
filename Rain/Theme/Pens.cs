using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Paint;

namespace Rain.Theme
{
    internal class Pens
    {
        public static readonly IPenInfo GradientHandleOutline = new PenInfo(1, Colors.GradientHandleOutline);

        public static readonly IPenInfo GradientHandleSelectedOutline =
            new PenInfo(1, Colors.GradientHandleSelectedOutline);

        public static readonly IPenInfo NodeSpecialOutline        = new PenInfo(1, Colors.NodeSpecialOutline);
        public static readonly IPenInfo NodeOutline               = new PenInfo(1, Colors.NodeOutline);
        public static readonly IPenInfo NodeOutlineAlt            = new PenInfo(1, Colors.NodeOutlineAlt);
        public static readonly IPenInfo SelectionOutline          = new PenInfo(1, Colors.SelectionOutline);
        public static readonly IPenInfo SelectionReferenceOutline = new PenInfo(1, Colors.SelectionReferenceOutline);
        public static readonly IPenInfo Guide                     = new PenInfo(2, Colors.Guide);
        public static readonly IPenInfo TextCaret                 = new PenInfo(1, Colors.TextCaret);
    }
}