using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Mathematics;

namespace Ibinimator.Svg
{
    public class CubicPathNode : PathNode
    {
        public Vector2 Control1 { get; set; }

        public Vector2 Control2 { get; set; }
    }
}