using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public class QuadraticPathNode : PathNode
    {
        public Vector2 Control
        {
            get => Get<Vector2>();
            set => Set(value);
        }
    }
}