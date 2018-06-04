using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Formatter.Svg.Structure;
using Rain.Formatter.Svg.Utilities;

namespace Rain.Formatter.Svg.Paint
{
    public abstract class Paint : ElementBase
    {
        private static readonly Regex Url = new Regex(@"url\((.+)\)", RegexOptions.Compiled);
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
            if (string.IsNullOrWhiteSpace(input))
            {
                paint = null;

                return false;
            }

            if (Color.TryParse(input, out var color))
            {
                paint = new SolidColorPaint(null, color);

                return true;
            }

            var match = Url.Match(input);

            if (match != null)
                if (UriHelper.TryParse(match.Groups[1].Value, out var uri))
                {
                    paint = new ReferencePaint {Reference = uri};

                    return true;
                }

            paint = null;

            return false;
        }
    }
}