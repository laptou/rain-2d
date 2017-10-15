using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using SharpDX.Mathematics.Interop;

namespace Ibinimator.Renderer
{
    public struct RectangleF
    {
        public float Height;
        public float Left;
        public float Top;
        public float Width;

        public RectangleF(float left, float top, float width, float height)
        {
            Top = top;
            Left = left;
            Width = width;
            Height = height;
        }

        public float Bottom
        {
            get => Top + Height;
            set => Height = value - Top;
        }

        public Vector2 BottomLeft => new Vector2(Left, Bottom);
        public Vector2 BottomRight => new Vector2(Right, Top);

        public Vector2 Center => new Vector2((Right + Left) / 2, (Bottom + Top) / 2);

        public static RectangleF Empty => (0, 0, 0, 0);

        public float Right
        {
            get => Left + Width;
            set => Width = value - Left;
        }

        public Vector2 TopLeft => new Vector2(Left, Top);
        public Vector2 TopRight => new Vector2(Right, Top);

        public bool IsEmpty => 
            Math.Abs(Left - Right) < float.Epsilon && 
            Math.Abs(Top - Bottom) < float.Epsilon;

        public bool Contains(Vector2 v)
        {
            return Left <= v.X && v.X <= Right &&
                   Top <= v.Y && v.Y <= Bottom;
        }

        public bool Contains(RectangleF r)
        {
            return Left <= r.Left && r.Right <= Right &&
                   r.Bottom <= Bottom && Top <= r.Top;
        }

        public static RectangleF Union(RectangleF r1, RectangleF r2)
        {
            return (
                Math.Min(r1.Left, r2.Left), Math.Min(r1.Top, r2.Top),
                Math.Max(r1.Right, r2.Right), Math.Max(r1.Bottom, r2.Bottom));
        }

        public static implicit operator RawRectangleF(RectangleF rect)
        {
            return new RawRectangleF(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        public static implicit operator (float Left, float Top, float Right, float Bottom)(RectangleF rect)
        {
            return (rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        public static implicit operator RectangleF((float Left, float Top, float Right, float Bottom) rect)
        {
            return new RectangleF(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        public void Offset(Vector2 delta)
        {
            Left += delta.X;
            Top += delta.Y;
        }
    }
}