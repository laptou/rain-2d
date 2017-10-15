using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Ibinimator.Renderer.Model
{
    public class PathNode : Model
    {
        public Vector2 Position
        {
            get => new Vector2(X, Y);
            set => (X, Y) = (value.X, value.Y);
        }

        [XmlAttribute]
        public float X
        {
            get => Get<float>();
            set => Set(value);
        }

        [XmlAttribute]
        public float Y
        {
            get => Get<float>();
            set => Set(value);
        }
    }
}