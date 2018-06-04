using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rain.Core.Utility
{
    public static class CurveAnalysis
    {
        #region Methods

        /// <summary>
        ///     Returns cubic control points that represent an equivalent curve
        ///     to the given quadratic control points.
        /// </summary>
        /// <param name="p0">The start point of the quadratic curve.</param>
        /// <param name="p1">The control point of the quadratic curve.</param>
        /// <param name="p2">The end point of the quadratic curve.</param>
        /// <returns>
        ///     4 points representing the control points of an
        ///     equivalent cubic curve.
        /// </returns>
        public static Vector2[] Cubic(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            return new[]
            {
                p0, Vector2.Lerp(p0, p1, 2f / 3), Vector2.Lerp(p2, p1, 2f / 3), p2
            };
        }

        #endregion Methods

        #region Nested type: DeCasteljau

        #region Classes

        public static class DeCasteljau
        {
            #region Methods

            public static Vector2 Interpolate(float f, params Vector2[] controls)
            {
                var c = controls.ToArray();

                while (c.Length > 2)
                {
                    var l = new Vector2[c.Length - 1];

                    for (var i = 0; i < c.Length - 1; i++)
                        l[i] = Vector2.Lerp(c[i], c[i + 1], f);

                    c = l;
                }

                return Vector2.Lerp(c[0], c[1], f);
            }

            public static Vector2 Interpolate(float f, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
            {
                var p01 = Vector2.Lerp(p0, p1, f);
                var p12 = Vector2.Lerp(p1, p2, f);
                var p23 = Vector2.Lerp(p2, p3, f);

                var p02 = Vector2.Lerp(p01, p12, f);
                var p13 = Vector2.Lerp(p12, p23, f);

                return Vector2.Lerp(p02, p13, f);
            }

            public static Vector2 Interpolate(float f, Vector2 p0, Vector2 p1, Vector2 p2)
            {
                var p01 = Vector2.Lerp(p0, p1, f);
                var p12 = Vector2.Lerp(p1, p2, f);

                return Vector2.Lerp(p01, p12, f);
            }

            public static (Vector2 c, Vector2[] s1, Vector2[] s2) Subdivide(float f, params Vector2[] controls)
            {
                var c = controls.ToArray();

                var s1 = new Vector2[c.Length];
                var s2 = new Vector2[c.Length];

                var j = 0;

                while (c.Length > 2)
                {
                    var l = new Vector2[c.Length - 1];

                    s1[j] = c[0];
                    s2[s2.Length - j] = c[c.Length - 1];

                    for (var i = 0; i < c.Length - 1; i++)
                        l[i] = Vector2.Lerp(c[i], c[i + 1], f);

                    c = l;
                    j++;
                }

                var x = Vector2.Lerp(c[0], c[1], f);

                s1[j] = s2[0] = x;

                return (x, s1, s2);
            }

            public static (Vector2 c, Vector2[] s1, Vector2[] s2) Subdivide(
                float f, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
            {
                var p01 = Vector2.Lerp(p0, p1, f);
                var p12 = Vector2.Lerp(p1, p2, f);
                var p23 = Vector2.Lerp(p2, p3, f);

                var p02 = Vector2.Lerp(p01, p12, f);
                var p13 = Vector2.Lerp(p12, p23, f);

                var c = Vector2.Lerp(p02, p13, f);

                var s1 = new[] {p0, p01, p02, c};
                var s2 = new[] {c, p23, p13, p3};

                return (c, s1, s2);
            }

            public static (Vector2 c, Vector2[] s1, Vector2[] s2) Subdivide(float f, Vector2 p0, Vector2 p1, Vector2 p2)
            {
                var p01 = Vector2.Lerp(p0, p1, f);
                var p12 = Vector2.Lerp(p1, p2, f);

                var c = Vector2.Lerp(p01, p12, f);

                var s1 = new[] {p0, p01, c};
                var s2 = new[] {c, p12, p2};

                return (c, s1, s2);
            }

            #endregion Methods
        }

        #endregion Classes

        #endregion
    }
}