using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model;

namespace Rain.Core.Utility
{
    public class EpsilonComparer : IEqualityComparer<float>, IEqualityComparer<Vector2>, IEqualityComparer<Matrix3x2>, IEqualityComparer<Color>
    {
        public static readonly EpsilonComparer Instance = new EpsilonComparer();

        #region IEqualityComparer<float> Members

        public bool Equals(float x, float y) { return Math.Abs(x - y) < MathUtils.Epsilon; }

        public int GetHashCode(float obj) { return obj.GetHashCode(); }

        #endregion

        #region IEqualityComparer<Vector2> Members

        public bool Equals(Vector2 x, Vector2 y)
        {
            return Math.Abs(x.X - y.X) < MathUtils.Epsilon && Math.Abs(x.Y - y.Y) < MathUtils.Epsilon;
        }

        public int GetHashCode(Vector2 obj) { return obj.GetHashCode(); }

        #endregion

        /// <inheritdoc />
        public bool Equals(Matrix3x2 x, Matrix3x2 y)
        {
            return Math.Abs(x.M11 - y.M11) < MathUtils.Epsilon && Math.Abs(x.M21 - y.M21) < MathUtils.Epsilon &&
                   Math.Abs(x.M31 - y.M31) < MathUtils.Epsilon && Math.Abs(x.M12 - y.M12) < MathUtils.Epsilon &&
                   Math.Abs(x.M22 - y.M22) < MathUtils.Epsilon && Math.Abs(x.M32 - y.M32) < MathUtils.Epsilon;
        }

        /// <inheritdoc />
        public int GetHashCode(Matrix3x2 obj) { return obj.GetHashCode(); }

        /// <inheritdoc />
        public bool Equals(Color x, Color y)
        {
            return Math.Abs(x.Red - y.Red) < MathUtils.Epsilon && Math.Abs(x.Green - y.Green) < MathUtils.Epsilon &&
                   Math.Abs(x.Blue - y.Blue) < MathUtils.Epsilon && Math.Abs(x.Alpha - y.Alpha) < MathUtils.Epsilon; }

        /// <inheritdoc />
        public int GetHashCode(Color obj) { return obj.GetHashCode(); }
    }
}