using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using static Ibinimator.Core.Mathematics.MathUtils;

namespace Ibinimator.Direct2D
{
    public static class SharpDXExtensions
    {
        public static (Vector2 scale, float rotation, Vector2 translation, float skew) Decompose(this Matrix3x2 m)
        {
            var scale = new Vector2(
                (float)Math.Sqrt(m.M11 * m.M11 + m.M12 * m.M12),
                (float)Math.Sqrt(m.M21 * m.M21 + m.M22 * m.M22));

            var translation = m.Row3;
            var skewx = (float)Math.Atan2(m.M12, m.M11);
            var skewy = (float)Math.Atan2(m.M22, m.M21) - PiOverTwo;
            var rotation = Wrap(skewx, TwoPi);
            var skew = Wrap(skewy - skewx, TwoPi);
            scale.Y *= (float)Math.Cos(skew);

            return (scale, rotation, translation, -skew);
        }
    }
}
