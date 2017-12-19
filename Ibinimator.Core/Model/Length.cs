using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model
{
    public struct Length
    {
        private static readonly Regex Pattern = new Regex(
            @"((?:[+-]?[0-9]*\.[0-9]+(?:[Ee][+-]?[0-9]+)?)|(?:(?:[+-]?[0-9]+)(?:[Ee][+-]?[0-9]+)?))(em|ex|px|in|cm|mm|pt|pc|%)?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Dictionary<LengthUnit, float> Factors = new Dictionary<LengthUnit, float>
        {
            [LengthUnit.Number] = 1f,
            [LengthUnit.Pixels] = 1f,
            [LengthUnit.Points] = 1.25f,
            [LengthUnit.Inches] = 96f
        };

        public static Length Zero = (0, LengthUnit.Number);

        public static Length Inherit = (0, LengthUnit.Inherit);

        public Length(float magnitude, LengthUnit unit) : this()
        {
            Magnitude = magnitude;
            Unit = unit;
        }

        public float Magnitude { get; }

        public LengthUnit Unit { get; }

        public static Length Convert(Length length, LengthUnit target)
        {
            return new Length(length.Magnitude * Factors[target] / Factors[length.Unit], target);
        }

        public bool Equals(Length other) { return Magnitude.Equals(other.Magnitude) && Unit == other.Unit; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;

            return obj is Length length && Equals(length);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Magnitude.GetHashCode() * 397) ^ (int) Unit;
            }
        }

        public static Length Parse(string input)
        {
            if (TryParse(input, out var length)) return length;

            throw new FormatException();
        }

        public float To(LengthUnit target) { return Convert(this, target).Magnitude; }

        public float To(LengthUnit target, float baseline)
        {
            if (Unit == LengthUnit.Percent)
                return Magnitude / 100 * baseline;

            if (Unit == LengthUnit.Inherit)
                return baseline;

            return To(target);
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

        public static bool TryParse(string input, out Length length)
        {
            length = Zero;

            if (string.IsNullOrWhiteSpace(input)) return false;

            var match = Pattern.Match(input);

            if (match.Success)
            {
                LengthUnit unit;

                switch (match.Groups[2].Value)
                {
                    case "em":
                        unit = LengthUnit.Ems;

                        break;
                    case "ex":
                        unit = LengthUnit.Exs;

                        break;
                    case "px":
                        unit = LengthUnit.Pixels;

                        break;
                    case "in":
                        unit = LengthUnit.Inches;

                        break;
                    case "cm":
                        unit = LengthUnit.Centimeter;

                        break;
                    case "mm":
                        unit = LengthUnit.Millimeter;

                        break;
                    case "pt":
                        unit = LengthUnit.Points;

                        break;
                    case "pc":
                        unit = LengthUnit.Picas;

                        break;
                    case "%":
                        unit = LengthUnit.Percent;

                        break;
                    case "":
                        unit = LengthUnit.Number;

                        break;
                    default: throw new FormatException("Invalid unit.");
                }

                length = new Length(float.Parse(match.Groups[1].Value), unit);

                return true;
            }

            return false;
        }

        public static bool operator ==(Length l1, Length l2)
        {
            var l3 = Convert(l2, l1.Unit);

            return Math.Abs(l1.Magnitude - l3.Magnitude) < float.Epsilon;
        }

        public static implicit operator Length(ValueTuple<float, LengthUnit> v)
        {
            return new Length(v.Item1, v.Item2);
        }

        public static bool operator !=(Length l1, Length l2) { return !(l1 == l2); }
    }
}