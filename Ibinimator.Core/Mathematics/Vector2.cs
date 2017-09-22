using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core.Mathematics
{
    public struct Vector2
    {
        public static Vector2 Zero = new Vector2();

        public Vector2(float x, float y) : this()
        {
            X = x;
            Y = y;
        }

        public float X { get; set; }
        public float Y { get; set; }

        public static float Dot(Vector2 v1, Vector2 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }

        public static Vector2 Max(Vector2 v1, Vector2 v2)
        {
            return new Vector2(Math.Max(v1.X, v2.X), Math.Max(v1.Y, v2.Y));
        }

        public static Vector2 Min(Vector2 v1, Vector2 v2)
        {
            return new Vector2(Math.Min(v1.X, v2.X), Math.Min(v1.Y, v2.Y));
        }

        public override string ToString()
        {
            return $"{X},{Y}";
        }

        public static Vector2 operator +(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.X + v2.X, v1.Y + v2.Y);
        }

        public static Vector2 operator +(Vector2 v1, float f)
        {
            return new Vector2(v1.X + f, v1.Y + f);
        }

        public static Vector2 operator /(Vector2 v1, float f)
        {
            return new Vector2(v1.X / f, v1.Y / f);
        }

        public static Vector2 operator *(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.X * v2.X, v1.Y * v2.Y);
        }

        public static Vector2 operator *(Vector2 v1, float f)
        {
            return new Vector2(v1.X * f, v1.Y * f);
        }

        public static Vector2 operator -(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.X - v2.X, v1.Y - v2.Y);
        }

        public static Vector2 operator -(Vector2 v1, float f)
        {
            return new Vector2(v1.X - f, v1.Y - f);
        }

        public static Vector2 operator -(Vector2 v1)
        {
            return new Vector2(-v1.X, -v1.Y);
        }
    }
}