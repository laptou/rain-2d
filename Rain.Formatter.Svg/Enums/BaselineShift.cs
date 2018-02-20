using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Measurement;
using Rain.Formatter.Svg.Utilities;

namespace Rain.Formatter.Svg.Enums
{
    public struct BaselineShift
    {
        #region Value enum

        public enum Value
        {
            Baseline,
            Sub,
            Super
        }

        #endregion

        public Value Enum { get; set; }

        public Length Length { get; set; }

        public override string ToString()
        {
            return Length != default ? Length.ToString() : Enum.Svgify();
        }
    }
}