using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg
{
    public class PathNode
    {
        public Vector2 Position
        {
            get => new Vector2(X, Y);
            set => (X, Y) = (value.X, value.Y);
        }

        public float X { get; set; }

        public float Y { get; set; }
    }
}