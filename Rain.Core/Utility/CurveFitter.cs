using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rain.Core.Utility
{
    public static class CurveFitter
    {
        #region Nested type: Bezier

        private struct Bezier
        {
            public Vector2 C1;
            public Vector2 C2;
            public Vector2 X1;
            public Vector2 X2;
        }

        #endregion

        //public IEnumerable<Bezier> FitCurve(Vector2 points, float maxError)
        //{

        //}
    }
}