using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Svg.Mathematics;

namespace Ibinimator.Svg
{
    public class QuadraticPathNode : PathNode
    {
        public Vector2 Control { get; set; }
    }
}