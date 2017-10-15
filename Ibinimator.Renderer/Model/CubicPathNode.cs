using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public class CubicPathNode : PathNode
    {
        public Vector2 Control1
        {
            get => Get<Vector2>();
            set => Set(value);
        }

        public Vector2 Control2
        {
            get => Get<Vector2>();
            set => Set(value);
        }
    }
}