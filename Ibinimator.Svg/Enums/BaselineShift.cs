using System;
using System.Collections.Generic;

using Ibinimator.Svg.Utilities;

using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Measurement;

namespace Ibinimator.Svg.Enums
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