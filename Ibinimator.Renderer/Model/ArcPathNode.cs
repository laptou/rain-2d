using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public class ArcPathNode : PathNode
    {
        public bool Clockwise
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LargeArc
        {
            get => Get<bool>();
            set => Set(value);
        }

        public float RadiusX
        {
            get => Get<float>();
            set => Set(value);
        }

        public float RadiusY
        {
            get => Get<float>();
            set => Set(value);
        }

        public float Rotation
        {
            get => Get<float>();
            set => Set(value);
        }
    }
}