using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model;
using Ibinimator.Formatter.Svg.Structure;

namespace Ibinimator.Formatter.Svg.Paint
{
    public abstract class Paint : Element
    {
        public virtual float Opacity { get; set; }

        public abstract string ToInline();

        public static Paint Parse(string input)
        {
            if (TryParse(input, out var paint)) return paint;

            throw new FormatException();
        }

        /// <inheritdoc />
        public override string ToString() { return ToInline(); }

        public static bool TryParse(string input, out Paint paint)
        {
            if (Color.TryParse(input, out var color))
            {
                paint = new SolidColorPaint(null, color);

                return true;
            }

            if (Iri.TryParse(input, out var iri))
            {
                paint = new ReferencePaint {Reference = iri};

                return true;
            }

            paint = null;

            return false;
        }
    }
}