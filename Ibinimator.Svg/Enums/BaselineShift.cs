using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Measurement;
using Ibinimator.Formatter.Svg.Utilities;

namespace Ibinimator.Formatter.Svg.Enums
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