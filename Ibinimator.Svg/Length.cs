using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ibinimator.Svg
{
    public struct Length
    {
        private static readonly Regex Pattern = new Regex(@"((?:[+-]?[0-9]*\.[0-9]+(?:[Ee][+-]?[0-9]+)?)|(?:(?:[+-]?[0-9]+)(?:[Ee][+-]?[0-9]+)?))(em|ex|px|in|cm|mm|pt|pc|%)?", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public Length(float magnitude, LengthUnit unit) : this()
        {
            Magnitude = magnitude;
            Unit = unit;
        }

        public float Magnitude { get; set; }

        public LengthUnit Unit { get; set; }

        public static Length Convert(Length length, LengthUnit target)
        {
            throw new NotImplementedException();
        }

        public static Length Parse(string input)
        {
            var match = Pattern.Match(input);

            if (match.Success)
            {
                LengthUnit unit;

                switch (match.Groups[2].Value)
                {
                    case "em": unit = LengthUnit.Ems; break;
                    case "ex": unit = LengthUnit.Exs; break;
                    case "px": unit = LengthUnit.Pixels; break;
                    case "in": unit = LengthUnit.Inches; break;
                    case "cm": unit = LengthUnit.Centimeter; break;
                    case "mm": unit = LengthUnit.Millimeter; break;
                    case "pt": unit = LengthUnit.Points; break;
                    case "pc": unit = LengthUnit.Picas; break;
                    case "%": unit = LengthUnit.Percent; break;
                    default: throw new FormatException("Invalid unit.");
                }

                return new Length(float.Parse(match.Groups[1].Value), unit);
            }

            throw new FormatException("Invalid length.");
        }

        public override string ToString()
        {
            var suffix = "";

            switch (Unit)
            {
                case LengthUnit.Number:
                    break;
                case LengthUnit.Percent:
                    suffix = "%";
                    break;
                case LengthUnit.Ems:
                    suffix = "em";
                    break;
                case LengthUnit.Exs:
                    suffix = "ex";
                    break;
                case LengthUnit.Pixels:
                    suffix = "px";
                    break;
                case LengthUnit.Centimeter:
                    suffix = "cm";
                    break;
                case LengthUnit.Millimeter:
                    suffix = "mm";
                    break;
                case LengthUnit.Inches:
                    suffix = "in";
                    break;
                case LengthUnit.Points:
                    suffix = "pt";
                    break;
                case LengthUnit.Picas:
                    suffix = "pc";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Magnitude + suffix;
        }

        public static implicit operator Length((float magnitude, LengthUnit unit) v)
        {
            return new Length(v.magnitude, v.unit);
        }

        public static bool operator ==(Length l1, Length l2)
        {
            var l3 = Convert(l2, l1.Unit);

            return Math.Abs(l1.Magnitude - l3.Magnitude) < C.Epsilon;
        }

        public static bool operator !=(Length l1, Length l2)
        {
            return !(l1 == l2);
        }

        public float To(LengthUnit target)
        {
            return Convert(this, target).Magnitude;
        }
    }
}