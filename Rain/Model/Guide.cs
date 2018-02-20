using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Renderer;

namespace Rain.Model
{
    public struct Guide
    {
        public Guide(int id, bool @virtual, Vector2 origin, float angle, GuideType type) : this()
        {
            Id = id;
            Virtual = @virtual;
            Origin = origin;
            Angle = angle;
            Type = type;
        }

        public Guide(
            int id, bool @virtual, Vector2 origin, float angle, int divisions,
            GuideType type) : this()
        {
            Id = id;
            Virtual = @virtual;
            Origin = origin;
            Angle = angle;
            Divisions = divisions;
            Type = type;
        }

        public float Angle { get; }

        public int Divisions { get; }

        public int Id { get; }

        public Vector2 Origin { get; }

        public GuideType Type { get; }

        public bool Virtual { get; }

        public override bool Equals(object obj)
        {
            if (obj is Guide g)
                return Equals(g);

            return base.Equals(obj);
        }

        public bool Equals(Guide g) { return g.Id == Id && g.Virtual == Virtual && g.Type == Type; }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode * 397) ^ Virtual.GetHashCode();
                hashCode = (hashCode * 397) ^ Origin.GetHashCode();
                hashCode = (hashCode * 397) ^ Angle.GetHashCode();
                hashCode = (hashCode * 397) ^ Divisions;
                hashCode = (hashCode * 397) ^ (int) Type;

                return hashCode;
            }
        }
    }
}