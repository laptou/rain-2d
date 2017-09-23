using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Svg
{
    public class CubicPathNode : PathNode
    {
        public Vector2 Control1 { get; set; }

        public Vector2 Control2 { get; set; }
    }
}