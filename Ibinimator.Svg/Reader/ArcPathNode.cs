using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg.Reader
{
    public class ArcPathNode : PathNode
    {
        public bool Clockwise { get; set; }

        public bool LargeArc { get; set; }

        public float RadiusX { get; set; }

        public float RadiusY { get; set; }

        public float Rotation { get; set; }
    }
}