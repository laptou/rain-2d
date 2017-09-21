using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg.Reader
{
    public struct BaselineShift
    {
        public enum Value
        {
            Baseline,
            Sub,
            Super
        }

        public Value Enum { get; set; }

        public Length Length { get; set; }

        public override string ToString()
        {
            return Length != default ? Length.ToString() : Enum.Svgify();
        }
    }
}