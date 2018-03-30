using System;
using System.Collections.Generic;
using System.Numerics;

namespace Rain.Core.Utility
{
    public static class EqualityComparers
    {
        public static readonly IEqualityComparer<float> Epsilon = new EpsilonComparer();
        public static readonly IEqualityComparer<Vector2> VectorEpsilon = new EpsilonVectorComparer();

        class EpsilonComparer : IEqualityComparer<float>
        {
            public bool Equals(float x, float y)
            {
                return Math.Abs(x - y) < MathUtils.Epsilon;
            }

            public int GetHashCode(float obj)
            {
                return obj.GetHashCode();
            }
        }

        class EpsilonVectorComparer : IEqualityComparer<Vector2>
        {
            public bool Equals(Vector2 x, Vector2 y)
            {
                return Math.Abs(x.X - y.X) < MathUtils.Epsilon &&
                       Math.Abs(x.Y - y.Y) < MathUtils.Epsilon;
            }

            public int GetHashCode(Vector2 obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}