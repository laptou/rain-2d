using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg
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

        public static Paint Parse(string input)
        {
            var color = Svg.Color.TryParse(input);

            if (color != null) return new Paint(color.Value);

            var iri = Svg.Iri.TryParse(input);

            if (iri != null) return new Paint(color.Value);

            throw new FormatException();
        }
    }
}