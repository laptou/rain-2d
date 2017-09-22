using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core.Mathematics
{
    public struct RectF
    {
        public RectF(float left, float top, float right, float bottom) : this()
        {
            Top = top;
            Right = right;
            Left = left;
            Bottom = bottom;
        }

        public RectF(Vector2 topLeft, Vector2 bottomRight)
            : this(topLeft.Y, topLeft.X, bottomRight.X, bottomRight.Y)
        {
        }

        public float Bottom { get; }
        public Vector2 BottomLeft => new Vector2(Bottom, Left);
        public Vector2 BottomRight => new Vector2(Bottom, Right);

        public float Height => Bottom - Top;
        public float Left { get; }
        public float Right { get; }

        public float Top { get; }
        public Vector2 TopLeft => new Vector2(Top, Left);

        public Vector2 TopRight => new Vector2(Top, Right);
        public float Width => Right - Left;

        public float X => Left;
        public float Y => Top;

        public RectF Inflate(float amount)
        {
            return Inflate(this, amount);
        }

        public RectF Inflate(Vector2 amount)
        {
            return Inflate(this, amount);
        }

        public static RectF Inflate(RectF rect, float amount)
        {
            return new RectF(rect.TopLeft - amount, rect.BottomRight + amount);
        }

        public static RectF Inflate(RectF rect, Vector2 amount)
        {
            return new RectF(rect.TopLeft - amount, rect.BottomRight + amount);
        }

        public RectF Intersection(RectF other)
        {
            return Intersection(this, other);
        }

        public static RectF Intersection(RectF r1, RectF r2)
        {
            return new RectF(
                Vector2.Max(r1.TopLeft, r2.TopLeft),
                Vector2.Min(r1.BottomRight, r2.BottomRight));
        }

        public RectF Offset(Vector2 delta)
        {
            return Offset(this, delta);
        }

        public static RectF Offset(RectF rect, Vector2 delta)
        {
            return new RectF(rect.TopLeft + delta, rect.BottomRight + delta);
        }

        public RectF Union(RectF other)
        {
            return Union(this, other);
        }

        public static RectF Union(RectF r1, RectF r2)
        {
            return new RectF(
                Vector2.Min(r1.TopLeft, r2.TopLeft),
                Vector2.Max(r1.BottomRight, r2.BottomRight));
        }
    }
}