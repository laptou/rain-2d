using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg
{
    internal static class X
    {
        public static string Svgify(this Enum enumeration)
        {
            var name = enumeration.ToString();
            var words = new List<string>();
            var current = "";

            foreach (var character in name)
            {
                if (char.IsUpper(character) && current != "")
                {
                    words.Add(current);
                    current = "";
                }

                current += character;
            }

            return string.Join("-", words);
        }

        public static string Svgify(this RectangleF rect)
        {
            return $"rect({rect.Left}px, {rect.Top}px, {rect.Right}px, {rect.Bottom} px";
        }
    }
}