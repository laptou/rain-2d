using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg.Reader
{
    public struct Paint
    {
        public Paint(Color color) : this()
        {
            Color = color;
        }

        public Paint(Iri iri) : this()
        {
            Iri = iri;
        }

        public Color? Color { get; set; }

        public Iri? Iri { get; set; }

        public static readonly Paint Black = new Paint(new Color());
    }
}