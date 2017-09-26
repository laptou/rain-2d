using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg
{
    public struct Paint
    {
        public static readonly Paint Black = new Paint(new Color());

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

        public static Paint Parse(string input)
        {
            if (TryParse(input, out var paint)) return paint;

            throw new FormatException();
        }

        public static bool TryParse(string input, out Paint paint)
        {
            var color = Svg.Color.TryParse(input);

            if (color != null)
            {
                paint = new Paint(color.Value);
                return true;
            }

            //var iri = Svg.Iri.TryParse(input);

            //if (iri != null)
            //{
            //    paint = new Paint(iri.Value);
            //    return true;
            //}

            paint = new Paint();

            return false;
        }

        public override string ToString()
        {
            if (Color != null) return Color.ToString();

            if (Iri != null) return Iri.ToString();

            return "none";
        }
    }
}