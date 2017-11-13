using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Service
{
    internal struct Guide
    {
        public Guide(bool @virtual, Vector2 origin, float angle) : this()
        {
            Virtual = @virtual;
            Origin = origin;
            Angle = angle;
        }

        public bool Virtual { get; }

        public Vector2 Origin { get; }

        public float Angle { get; }
    }
}