using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rain.Core.Utility
{
    public static class EqualityComparers
    {
        public static readonly IEqualityComparer<float>   Epsilon       = new EpsilonComparer();
        public static readonly IEqualityComparer<Vector2> VectorEpsilon = new EpsilonVectorComparer();

        #region Nested type: EpsilonComparer

        private class EpsilonComparer : IEqualityComparer<float>
        {
            #region IEqualityComparer<float> Members

            public bool Equals(float x, float y) { return Math.Abs(x - y) < MathUtils.Epsilon; }

            public int GetHashCode(float obj) { return obj.GetHashCode(); }

            #endregion
        }

        #endregion

        #region Nested type: EpsilonVectorComparer

        private class EpsilonVectorComparer : IEqualityComparer<Vector2>
        {
            #region IEqualityComparer<Vector2> Members

            public bool Equals(Vector2 x, Vector2 y)
            {
                return Math.Abs(x.X - y.X) < MathUtils.Epsilon && Math.Abs(x.Y - y.Y) < MathUtils.Epsilon;
            }

            public int GetHashCode(Vector2 obj) { return obj.GetHashCode(); }

            #endregion
        }

        #endregion
    }
}