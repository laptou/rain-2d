using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ibinimator.Svg
{
    public struct Color
    {
        private static readonly Regex Hex = new Regex("(?:#(?:([0-9A-F]){3}){1,2})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex Rgb = new Regex(
            @"(?:rgb\(([+-]?[0-9]+)[\u0009\u000D\u000A]*,[\u0020\u0009\u000D\u000A]*([+-]?[0-9]+)[\u0020\u0009\u000D\u000A]*,[\u0020\u0009\u000D\u000A]*([+-]?[0-9]+)\))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex Percent = new Regex(
            @"(?:rgb\(([+-]?[0-9]+)%[\u0020\u0009\u000D\u000A]*,[\u0020\u0009\u000D\u000A]*([+-]?[0-9]+)%[\u0020\u0009\u000D\u000A]*,[\u0020\u0009\u000D\u000A]*([+-]?[0-9]+)%\))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public Color(float red, float green, float blue) : this(red, green, blue, 1)
        {
        }

        public Color(float red, float green, float blue, float alpha) : this()
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }

        public float Alpha { get; }

        public float Blue { get; }

        public float Green { get; }

        public float Red { get; }

        public static Color Parse(string input)
        {
            var hexMatch = Hex.Match(input);

            if (hexMatch.Success)
            {
                var digits = hexMatch.Groups[1].Captures.OfType<Capture>()
                    .Select(c => c.Value)
                    .ToArray();

                if (digits.Length == 3)
                    return new Color(
                        Convert.ToInt32(digits[0] + digits[0], 16) / 256f,
                        Convert.ToInt32(digits[1] + digits[1], 16) / 256f,
                        Convert.ToInt32(digits[2] + digits[2], 16) / 256f);

                if (digits.Length == 6)
                    return new Color(
                        Convert.ToInt32(digits[0] + digits[1], 16) / 256f,
                        Convert.ToInt32(digits[2] + digits[3], 16) / 256f,
                        Convert.ToInt32(digits[4] + digits[5], 16) / 256f);
            }

            var rgbMatch = Rgb.Match(input);

            if (rgbMatch.Success)
            {
                var values = rgbMatch.Groups[1].Captures.OfType<Capture>()
                    .Select(c => c.Value)
                    .ToArray();

                return new Color(
                    int.Parse(values[0]) / 256f,
                    int.Parse(values[1]) / 256f,
                    int.Parse(values[2]) / 256f);
            }

            var percentMatch = Percent.Match(input);

            if (percentMatch.Success)
            {
                var values = percentMatch.Groups[1].Captures.OfType<Capture>()
                    .Select(c => c.Value)
                    .ToArray();

                return new Color(
                    int.Parse(values[0]) / 100f,
                    int.Parse(values[1]) / 100f,
                    int.Parse(values[2]) / 100f);
            }

            switch (input.ToLowerInvariant())
            {
                case "aliceblue": return new Color(240 / 255f, 248 / 255f, 255 / 255f);
                case "antiquewhite": return new Color(250 / 255f, 235 / 255f, 215 / 255f);
                case "aqua": return new Color(0 / 255f, 255 / 255f, 255 / 255f);
                case "aquamarine": return new Color(127 / 255f, 255 / 255f, 212 / 255f);
                case "azure": return new Color(240 / 255f, 255 / 255f, 255 / 255f);
                case "beige": return new Color(245 / 255f, 245 / 255f, 220 / 255f);
                case "bisque": return new Color(255 / 255f, 228 / 255f, 196 / 255f);
                case "black": return new Color(0 / 255f, 0 / 255f, 0 / 255f);
                case "blanchedalmond": return new Color(255 / 255f, 235 / 255f, 205 / 255f);
                case "blue": return new Color(0 / 255f, 0 / 255f, 255 / 255f);
                case "blueviolet": return new Color(138 / 255f, 43 / 255f, 226 / 255f);
                case "brown": return new Color(165 / 255f, 42 / 255f, 42 / 255f);
                case "burlywood": return new Color(222 / 255f, 184 / 255f, 135 / 255f);
                case "cadetblue": return new Color(95 / 255f, 158 / 255f, 160 / 255f);
                case "chartreuse": return new Color(127 / 255f, 255 / 255f, 0 / 255f);
                case "chocolate": return new Color(210 / 255f, 105 / 255f, 30 / 255f);
                case "coral": return new Color(255 / 255f, 127 / 255f, 80 / 255f);
                case "cornflowerblue": return new Color(100 / 255f, 149 / 255f, 237 / 255f);
                case "cornsilk": return new Color(255 / 255f, 248 / 255f, 220 / 255f);
                case "crimson": return new Color(220 / 255f, 20 / 255f, 60 / 255f);
                case "cyan": return new Color(0 / 255f, 255 / 255f, 255 / 255f);
                case "darkblue": return new Color(0 / 255f, 0 / 255f, 139 / 255f);
                case "darkcyan": return new Color(0 / 255f, 139 / 255f, 139 / 255f);
                case "darkgoldenrod": return new Color(184 / 255f, 134 / 255f, 11 / 255f);
                case "darkgray": return new Color(169 / 255f, 169 / 255f, 169 / 255f);
                case "darkgreen": return new Color(0 / 255f, 100 / 255f, 0 / 255f);
                case "darkgrey": return new Color(169 / 255f, 169 / 255f, 169 / 255f);
                case "darkkhaki": return new Color(189 / 255f, 183 / 255f, 107 / 255f);
                case "darkmagenta": return new Color(139 / 255f, 0 / 255f, 139 / 255f);
                case "darkolivegreen": return new Color(85 / 255f, 107 / 255f, 47 / 255f);
                case "darkorange": return new Color(255 / 255f, 140 / 255f, 0 / 255f);
                case "darkorchid": return new Color(153 / 255f, 50 / 255f, 204 / 255f);
                case "darkred": return new Color(139 / 255f, 0 / 255f, 0 / 255f);
                case "darksalmon": return new Color(233 / 255f, 150 / 255f, 122 / 255f);
                case "darkseagreen": return new Color(143 / 255f, 188 / 255f, 143 / 255f);
                case "darkslateblue": return new Color(72 / 255f, 61 / 255f, 139 / 255f);
                case "darkslategray": return new Color(47 / 255f, 79 / 255f, 79 / 255f);
                case "darkslategrey": return new Color(47 / 255f, 79 / 255f, 79 / 255f);
                case "darkturquoise": return new Color(0 / 255f, 206 / 255f, 209 / 255f);
                case "darkviolet": return new Color(148 / 255f, 0 / 255f, 211 / 255f);
                case "deeppink": return new Color(255 / 255f, 20 / 255f, 147 / 255f);
                case "deepskyblue": return new Color(0 / 255f, 191 / 255f, 255 / 255f);
                case "dimgray": return new Color(105 / 255f, 105 / 255f, 105 / 255f);
                case "dimgrey": return new Color(105 / 255f, 105 / 255f, 105 / 255f);
                case "dodgerblue": return new Color(30 / 255f, 144 / 255f, 255 / 255f);
                case "firebrick": return new Color(178 / 255f, 34 / 255f, 34 / 255f);
                case "floralwhite": return new Color(255 / 255f, 250 / 255f, 240 / 255f);
                case "forestgreen": return new Color(34 / 255f, 139 / 255f, 34 / 255f);
                case "fuchsia": return new Color(255 / 255f, 0 / 255f, 255 / 255f);
                case "gainsboro": return new Color(220 / 255f, 220 / 255f, 220 / 255f);
                case "ghostwhite": return new Color(248 / 255f, 248 / 255f, 255 / 255f);
                case "gold": return new Color(255 / 255f, 215 / 255f, 0 / 255f);
                case "goldenrod": return new Color(218 / 255f, 165 / 255f, 32 / 255f);
                case "gray": return new Color(128 / 255f, 128 / 255f, 128 / 255f);
                case "grey": return new Color(128 / 255f, 128 / 255f, 128 / 255f);
                case "green": return new Color(0 / 255f, 128 / 255f, 0 / 255f);
                case "greenyellow": return new Color(173 / 255f, 255 / 255f, 47 / 255f);
                case "honeydew": return new Color(240 / 255f, 255 / 255f, 240 / 255f);
                case "hotpink": return new Color(255 / 255f, 105 / 255f, 180 / 255f);
                case "indianred": return new Color(205 / 255f, 92 / 255f, 92 / 255f);
                case "indigo": return new Color(75 / 255f, 0 / 255f, 130 / 255f);
                case "ivory": return new Color(255 / 255f, 255 / 255f, 240 / 255f);
                case "khaki": return new Color(240 / 255f, 230 / 255f, 140 / 255f);
                case "lavender": return new Color(230 / 255f, 230 / 255f, 250 / 255f);
                case "lavenderblush": return new Color(255 / 255f, 240 / 255f, 245 / 255f);
                case "lawngreen": return new Color(124 / 255f, 252 / 255f, 0 / 255f);
                case "lemonchiffon": return new Color(255 / 255f, 250 / 255f, 205 / 255f);
                case "lightblue": return new Color(173 / 255f, 216 / 255f, 230 / 255f);
                case "lightcoral": return new Color(240 / 255f, 128 / 255f, 128 / 255f);
                case "lightcyan": return new Color(224 / 255f, 255 / 255f, 255 / 255f);
                case "lightgoldenrodyellow": return new Color(250 / 255f, 250 / 255f, 210 / 255f);
                case "lightgray": return new Color(211 / 255f, 211 / 255f, 211 / 255f);
                case "lightgreen": return new Color(144 / 255f, 238 / 255f, 144 / 255f);
                case "lightgrey": return new Color(211 / 255f, 211 / 255f, 211 / 255f);
                case "lightpink": return new Color(255 / 255f, 182 / 255f, 193 / 255f);
                case "lightsalmon": return new Color(255 / 255f, 160 / 255f, 122 / 255f);
                case "lightseagreen": return new Color(32 / 255f, 178 / 255f, 170 / 255f);
                case "lightskyblue": return new Color(135 / 255f, 206 / 255f, 250 / 255f);
                case "lightslategray": return new Color(119 / 255f, 136 / 255f, 153 / 255f);
                case "lightslategrey": return new Color(119 / 255f, 136 / 255f, 153 / 255f);
                case "lightsteelblue": return new Color(176 / 255f, 196 / 255f, 222 / 255f);
                case "lightyellow": return new Color(255 / 255f, 255 / 255f, 224 / 255f);
                case "lime": return new Color(0 / 255f, 255 / 255f, 0 / 255f);
                case "limegreen": return new Color(50 / 255f, 205 / 255f, 50 / 255f);
                case "linen": return new Color(250 / 255f, 240 / 255f, 230 / 255f);
                case "magenta": return new Color(255 / 255f, 0 / 255f, 255 / 255f);
                case "maroon": return new Color(128 / 255f, 0 / 255f, 0 / 255f);
                case "mediumaquamarine": return new Color(102 / 255f, 205 / 255f, 170 / 255f);
                case "mediumblue": return new Color(0 / 255f, 0 / 255f, 205 / 255f);
                case "mediumorchid": return new Color(186 / 255f, 85 / 255f, 211 / 255f);
                case "mediumpurple": return new Color(147 / 255f, 112 / 255f, 219 / 255f);
                case "mediumseagreen": return new Color(60 / 255f, 179 / 255f, 113 / 255f);
                case "mediumslateblue": return new Color(123 / 255f, 104 / 255f, 238 / 255f);
                case "mediumspringgreen": return new Color(0 / 255f, 250 / 255f, 154 / 255f);
                case "mediumturquoise": return new Color(72 / 255f, 209 / 255f, 204 / 255f);
                case "mediumvioletred": return new Color(199 / 255f, 21 / 255f, 133 / 255f);
                case "midnightblue": return new Color(25 / 255f, 25 / 255f, 112 / 255f);
                case "mintcream": return new Color(245 / 255f, 255 / 255f, 250 / 255f);
                case "mistyrose": return new Color(255 / 255f, 228 / 255f, 225 / 255f);
                case "moccasin": return new Color(255 / 255f, 228 / 255f, 181 / 255f);
                case "navajowhite": return new Color(255 / 255f, 222 / 255f, 173 / 255f);
                case "navy": return new Color(0 / 255f, 0 / 255f, 128 / 255f);
                case "oldlace": return new Color(253 / 255f, 245 / 255f, 230 / 255f);
                case "olive": return new Color(128 / 255f, 128 / 255f, 0 / 255f);
                case "olivedrab": return new Color(107 / 255f, 142 / 255f, 35 / 255f);
                case "orange": return new Color(255 / 255f, 165 / 255f, 0 / 255f);
                case "orangered": return new Color(255 / 255f, 69 / 255f, 0 / 255f);
                case "orchid": return new Color(218 / 255f, 112 / 255f, 214 / 255f);
                case "palegoldenrod": return new Color(238 / 255f, 232 / 255f, 170 / 255f);
                case "palegreen": return new Color(152 / 255f, 251 / 255f, 152 / 255f);
                case "paleturquoise": return new Color(175 / 255f, 238 / 255f, 238 / 255f);
                case "palevioletred": return new Color(219 / 255f, 112 / 255f, 147 / 255f);
                case "papayawhip": return new Color(255 / 255f, 239 / 255f, 213 / 255f);
                case "peachpuff": return new Color(255 / 255f, 218 / 255f, 185 / 255f);
                case "peru": return new Color(205 / 255f, 133 / 255f, 63 / 255f);
                case "pink": return new Color(255 / 255f, 192 / 255f, 203 / 255f);
                case "plum": return new Color(221 / 255f, 160 / 255f, 221 / 255f);
                case "powderblue": return new Color(176 / 255f, 224 / 255f, 230 / 255f);
                case "purple": return new Color(128 / 255f, 0 / 255f, 128 / 255f);
                case "red": return new Color(255 / 255f, 0 / 255f, 0 / 255f);
                case "rosybrown": return new Color(188 / 255f, 143 / 255f, 143 / 255f);
                case "royalblue": return new Color(65 / 255f, 105 / 255f, 225 / 255f);
                case "saddlebrown": return new Color(139 / 255f, 69 / 255f, 19 / 255f);
                case "salmon": return new Color(250 / 255f, 128 / 255f, 114 / 255f);
                case "sandybrown": return new Color(244 / 255f, 164 / 255f, 96 / 255f);
                case "seagreen": return new Color(46 / 255f, 139 / 255f, 87 / 255f);
                case "seashell": return new Color(255 / 255f, 245 / 255f, 238 / 255f);
                case "sienna": return new Color(160 / 255f, 82 / 255f, 45 / 255f);
                case "silver": return new Color(192 / 255f, 192 / 255f, 192 / 255f);
                case "skyblue": return new Color(135 / 255f, 206 / 255f, 235 / 255f);
                case "slateblue": return new Color(106 / 255f, 90 / 255f, 205 / 255f);
                case "slategray": return new Color(112 / 255f, 128 / 255f, 144 / 255f);
                case "slategrey": return new Color(112 / 255f, 128 / 255f, 144 / 255f);
                case "snow": return new Color(255 / 255f, 250 / 255f, 250 / 255f);
                case "springgreen": return new Color(0 / 255f, 255 / 255f, 127 / 255f);
                case "steelblue": return new Color(70 / 255f, 130 / 255f, 180 / 255f);
                case "tan": return new Color(210 / 255f, 180 / 255f, 140 / 255f);
                case "teal": return new Color(0 / 255f, 128 / 255f, 128 / 255f);
                case "thistle": return new Color(216 / 255f, 191 / 255f, 216 / 255f);
                case "tomato": return new Color(255 / 255f, 99 / 255f, 71 / 255f);
                case "turquoise": return new Color(64 / 255f, 224 / 255f, 208 / 255f);
                case "violet": return new Color(238 / 255f, 130 / 255f, 238 / 255f);
                case "wheat": return new Color(245 / 255f, 222 / 255f, 179 / 255f);
                case "white": return new Color(255 / 255f, 255 / 255f, 255 / 255f);
                case "whitesmoke": return new Color(245 / 255f, 245 / 255f, 245 / 255f);
                case "yellow": return new Color(255 / 255f, 255 / 255f, 0 / 255f);
                case "yellowgreen": return new Color(154 / 255f, 205 / 255f, 50 / 255f);
                case "transparent":
                case "none": return new Color(0, 0, 0, 0);
                default: throw new FormatException("Invalid color.");
            }
        }

        public static Color? TryParse(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            var hexMatch = Hex.Match(input);

            if (hexMatch.Success)
            {
                var digits = hexMatch.Groups[1].Captures.OfType<Capture>()
                    .Select(c => c.Value)
                    .ToArray();

                if (digits.Length == 3)
                    return new Color(
                        Convert.ToInt32(digits[0] + digits[0], 16) / 256f,
                        Convert.ToInt32(digits[1] + digits[1], 16) / 256f,
                        Convert.ToInt32(digits[2] + digits[2], 16) / 256f);

                if (digits.Length == 6)
                    return new Color(
                        Convert.ToInt32(digits[0] + digits[1], 16) / 256f,
                        Convert.ToInt32(digits[2] + digits[3], 16) / 256f,
                        Convert.ToInt32(digits[4] + digits[5], 16) / 256f);
            }

            var rgbMatch = Rgb.Match(input);

            if (rgbMatch.Success)
            {
                var values = rgbMatch.Groups[1].Captures.OfType<Capture>()
                    .Select(c => c.Value)
                    .ToArray();

                return new Color(
                    int.Parse(values[0]) / 256f,
                    int.Parse(values[1]) / 256f,
                    int.Parse(values[2]) / 256f);
            }

            var percentMatch = Percent.Match(input);

            if (percentMatch.Success)
            {
                var values = percentMatch.Groups
                    .OfType<System.Text.RegularExpressions.Group>()
                    .Skip(1)
                    .Select(g => g.Value)
                    .ToArray();

                return new Color(
                    int.Parse(values[0]) / 100f,
                    int.Parse(values[1]) / 100f,
                    int.Parse(values[2]) / 100f);
            }

            switch (input.ToLowerInvariant())
            {
                case "aliceblue": return new Color(240 / 255f, 248 / 255f, 255 / 255f);
                case "antiquewhite": return new Color(250 / 255f, 235 / 255f, 215 / 255f);
                case "aqua": return new Color(0 / 255f, 255 / 255f, 255 / 255f);
                case "aquamarine": return new Color(127 / 255f, 255 / 255f, 212 / 255f);
                case "azure": return new Color(240 / 255f, 255 / 255f, 255 / 255f);
                case "beige": return new Color(245 / 255f, 245 / 255f, 220 / 255f);
                case "bisque": return new Color(255 / 255f, 228 / 255f, 196 / 255f);
                case "black": return new Color(0 / 255f, 0 / 255f, 0 / 255f);
                case "blanchedalmond": return new Color(255 / 255f, 235 / 255f, 205 / 255f);
                case "blue": return new Color(0 / 255f, 0 / 255f, 255 / 255f);
                case "blueviolet": return new Color(138 / 255f, 43 / 255f, 226 / 255f);
                case "brown": return new Color(165 / 255f, 42 / 255f, 42 / 255f);
                case "burlywood": return new Color(222 / 255f, 184 / 255f, 135 / 255f);
                case "cadetblue": return new Color(95 / 255f, 158 / 255f, 160 / 255f);
                case "chartreuse": return new Color(127 / 255f, 255 / 255f, 0 / 255f);
                case "chocolate": return new Color(210 / 255f, 105 / 255f, 30 / 255f);
                case "coral": return new Color(255 / 255f, 127 / 255f, 80 / 255f);
                case "cornflowerblue": return new Color(100 / 255f, 149 / 255f, 237 / 255f);
                case "cornsilk": return new Color(255 / 255f, 248 / 255f, 220 / 255f);
                case "crimson": return new Color(220 / 255f, 20 / 255f, 60 / 255f);
                case "cyan": return new Color(0 / 255f, 255 / 255f, 255 / 255f);
                case "darkblue": return new Color(0 / 255f, 0 / 255f, 139 / 255f);
                case "darkcyan": return new Color(0 / 255f, 139 / 255f, 139 / 255f);
                case "darkgoldenrod": return new Color(184 / 255f, 134 / 255f, 11 / 255f);
                case "darkgray": return new Color(169 / 255f, 169 / 255f, 169 / 255f);
                case "darkgreen": return new Color(0 / 255f, 100 / 255f, 0 / 255f);
                case "darkgrey": return new Color(169 / 255f, 169 / 255f, 169 / 255f);
                case "darkkhaki": return new Color(189 / 255f, 183 / 255f, 107 / 255f);
                case "darkmagenta": return new Color(139 / 255f, 0 / 255f, 139 / 255f);
                case "darkolivegreen": return new Color(85 / 255f, 107 / 255f, 47 / 255f);
                case "darkorange": return new Color(255 / 255f, 140 / 255f, 0 / 255f);
                case "darkorchid": return new Color(153 / 255f, 50 / 255f, 204 / 255f);
                case "darkred": return new Color(139 / 255f, 0 / 255f, 0 / 255f);
                case "darksalmon": return new Color(233 / 255f, 150 / 255f, 122 / 255f);
                case "darkseagreen": return new Color(143 / 255f, 188 / 255f, 143 / 255f);
                case "darkslateblue": return new Color(72 / 255f, 61 / 255f, 139 / 255f);
                case "darkslategray": return new Color(47 / 255f, 79 / 255f, 79 / 255f);
                case "darkslategrey": return new Color(47 / 255f, 79 / 255f, 79 / 255f);
                case "darkturquoise": return new Color(0 / 255f, 206 / 255f, 209 / 255f);
                case "darkviolet": return new Color(148 / 255f, 0 / 255f, 211 / 255f);
                case "deeppink": return new Color(255 / 255f, 20 / 255f, 147 / 255f);
                case "deepskyblue": return new Color(0 / 255f, 191 / 255f, 255 / 255f);
                case "dimgray": return new Color(105 / 255f, 105 / 255f, 105 / 255f);
                case "dimgrey": return new Color(105 / 255f, 105 / 255f, 105 / 255f);
                case "dodgerblue": return new Color(30 / 255f, 144 / 255f, 255 / 255f);
                case "firebrick": return new Color(178 / 255f, 34 / 255f, 34 / 255f);
                case "floralwhite": return new Color(255 / 255f, 250 / 255f, 240 / 255f);
                case "forestgreen": return new Color(34 / 255f, 139 / 255f, 34 / 255f);
                case "fuchsia": return new Color(255 / 255f, 0 / 255f, 255 / 255f);
                case "gainsboro": return new Color(220 / 255f, 220 / 255f, 220 / 255f);
                case "ghostwhite": return new Color(248 / 255f, 248 / 255f, 255 / 255f);
                case "gold": return new Color(255 / 255f, 215 / 255f, 0 / 255f);
                case "goldenrod": return new Color(218 / 255f, 165 / 255f, 32 / 255f);
                case "gray": return new Color(128 / 255f, 128 / 255f, 128 / 255f);
                case "grey": return new Color(128 / 255f, 128 / 255f, 128 / 255f);
                case "green": return new Color(0 / 255f, 128 / 255f, 0 / 255f);
                case "greenyellow": return new Color(173 / 255f, 255 / 255f, 47 / 255f);
                case "honeydew": return new Color(240 / 255f, 255 / 255f, 240 / 255f);
                case "hotpink": return new Color(255 / 255f, 105 / 255f, 180 / 255f);
                case "indianred": return new Color(205 / 255f, 92 / 255f, 92 / 255f);
                case "indigo": return new Color(75 / 255f, 0 / 255f, 130 / 255f);
                case "ivory": return new Color(255 / 255f, 255 / 255f, 240 / 255f);
                case "khaki": return new Color(240 / 255f, 230 / 255f, 140 / 255f);
                case "lavender": return new Color(230 / 255f, 230 / 255f, 250 / 255f);
                case "lavenderblush": return new Color(255 / 255f, 240 / 255f, 245 / 255f);
                case "lawngreen": return new Color(124 / 255f, 252 / 255f, 0 / 255f);
                case "lemonchiffon": return new Color(255 / 255f, 250 / 255f, 205 / 255f);
                case "lightblue": return new Color(173 / 255f, 216 / 255f, 230 / 255f);
                case "lightcoral": return new Color(240 / 255f, 128 / 255f, 128 / 255f);
                case "lightcyan": return new Color(224 / 255f, 255 / 255f, 255 / 255f);
                case "lightgoldenrodyellow": return new Color(250 / 255f, 250 / 255f, 210 / 255f);
                case "lightgray": return new Color(211 / 255f, 211 / 255f, 211 / 255f);
                case "lightgreen": return new Color(144 / 255f, 238 / 255f, 144 / 255f);
                case "lightgrey": return new Color(211 / 255f, 211 / 255f, 211 / 255f);
                case "lightpink": return new Color(255 / 255f, 182 / 255f, 193 / 255f);
                case "lightsalmon": return new Color(255 / 255f, 160 / 255f, 122 / 255f);
                case "lightseagreen": return new Color(32 / 255f, 178 / 255f, 170 / 255f);
                case "lightskyblue": return new Color(135 / 255f, 206 / 255f, 250 / 255f);
                case "lightslategray": return new Color(119 / 255f, 136 / 255f, 153 / 255f);
                case "lightslategrey": return new Color(119 / 255f, 136 / 255f, 153 / 255f);
                case "lightsteelblue": return new Color(176 / 255f, 196 / 255f, 222 / 255f);
                case "lightyellow": return new Color(255 / 255f, 255 / 255f, 224 / 255f);
                case "lime": return new Color(0 / 255f, 255 / 255f, 0 / 255f);
                case "limegreen": return new Color(50 / 255f, 205 / 255f, 50 / 255f);
                case "linen": return new Color(250 / 255f, 240 / 255f, 230 / 255f);
                case "magenta": return new Color(255 / 255f, 0 / 255f, 255 / 255f);
                case "maroon": return new Color(128 / 255f, 0 / 255f, 0 / 255f);
                case "mediumaquamarine": return new Color(102 / 255f, 205 / 255f, 170 / 255f);
                case "mediumblue": return new Color(0 / 255f, 0 / 255f, 205 / 255f);
                case "mediumorchid": return new Color(186 / 255f, 85 / 255f, 211 / 255f);
                case "mediumpurple": return new Color(147 / 255f, 112 / 255f, 219 / 255f);
                case "mediumseagreen": return new Color(60 / 255f, 179 / 255f, 113 / 255f);
                case "mediumslateblue": return new Color(123 / 255f, 104 / 255f, 238 / 255f);
                case "mediumspringgreen": return new Color(0 / 255f, 250 / 255f, 154 / 255f);
                case "mediumturquoise": return new Color(72 / 255f, 209 / 255f, 204 / 255f);
                case "mediumvioletred": return new Color(199 / 255f, 21 / 255f, 133 / 255f);
                case "midnightblue": return new Color(25 / 255f, 25 / 255f, 112 / 255f);
                case "mintcream": return new Color(245 / 255f, 255 / 255f, 250 / 255f);
                case "mistyrose": return new Color(255 / 255f, 228 / 255f, 225 / 255f);
                case "moccasin": return new Color(255 / 255f, 228 / 255f, 181 / 255f);
                case "navajowhite": return new Color(255 / 255f, 222 / 255f, 173 / 255f);
                case "navy": return new Color(0 / 255f, 0 / 255f, 128 / 255f);
                case "oldlace": return new Color(253 / 255f, 245 / 255f, 230 / 255f);
                case "olive": return new Color(128 / 255f, 128 / 255f, 0 / 255f);
                case "olivedrab": return new Color(107 / 255f, 142 / 255f, 35 / 255f);
                case "orange": return new Color(255 / 255f, 165 / 255f, 0 / 255f);
                case "orangered": return new Color(255 / 255f, 69 / 255f, 0 / 255f);
                case "orchid": return new Color(218 / 255f, 112 / 255f, 214 / 255f);
                case "palegoldenrod": return new Color(238 / 255f, 232 / 255f, 170 / 255f);
                case "palegreen": return new Color(152 / 255f, 251 / 255f, 152 / 255f);
                case "paleturquoise": return new Color(175 / 255f, 238 / 255f, 238 / 255f);
                case "palevioletred": return new Color(219 / 255f, 112 / 255f, 147 / 255f);
                case "papayawhip": return new Color(255 / 255f, 239 / 255f, 213 / 255f);
                case "peachpuff": return new Color(255 / 255f, 218 / 255f, 185 / 255f);
                case "peru": return new Color(205 / 255f, 133 / 255f, 63 / 255f);
                case "pink": return new Color(255 / 255f, 192 / 255f, 203 / 255f);
                case "plum": return new Color(221 / 255f, 160 / 255f, 221 / 255f);
                case "powderblue": return new Color(176 / 255f, 224 / 255f, 230 / 255f);
                case "purple": return new Color(128 / 255f, 0 / 255f, 128 / 255f);
                case "red": return new Color(255 / 255f, 0 / 255f, 0 / 255f);
                case "rosybrown": return new Color(188 / 255f, 143 / 255f, 143 / 255f);
                case "royalblue": return new Color(65 / 255f, 105 / 255f, 225 / 255f);
                case "saddlebrown": return new Color(139 / 255f, 69 / 255f, 19 / 255f);
                case "salmon": return new Color(250 / 255f, 128 / 255f, 114 / 255f);
                case "sandybrown": return new Color(244 / 255f, 164 / 255f, 96 / 255f);
                case "seagreen": return new Color(46 / 255f, 139 / 255f, 87 / 255f);
                case "seashell": return new Color(255 / 255f, 245 / 255f, 238 / 255f);
                case "sienna": return new Color(160 / 255f, 82 / 255f, 45 / 255f);
                case "silver": return new Color(192 / 255f, 192 / 255f, 192 / 255f);
                case "skyblue": return new Color(135 / 255f, 206 / 255f, 235 / 255f);
                case "slateblue": return new Color(106 / 255f, 90 / 255f, 205 / 255f);
                case "slategray": return new Color(112 / 255f, 128 / 255f, 144 / 255f);
                case "slategrey": return new Color(112 / 255f, 128 / 255f, 144 / 255f);
                case "snow": return new Color(255 / 255f, 250 / 255f, 250 / 255f);
                case "springgreen": return new Color(0 / 255f, 255 / 255f, 127 / 255f);
                case "steelblue": return new Color(70 / 255f, 130 / 255f, 180 / 255f);
                case "tan": return new Color(210 / 255f, 180 / 255f, 140 / 255f);
                case "teal": return new Color(0 / 255f, 128 / 255f, 128 / 255f);
                case "thistle": return new Color(216 / 255f, 191 / 255f, 216 / 255f);
                case "tomato": return new Color(255 / 255f, 99 / 255f, 71 / 255f);
                case "turquoise": return new Color(64 / 255f, 224 / 255f, 208 / 255f);
                case "violet": return new Color(238 / 255f, 130 / 255f, 238 / 255f);
                case "wheat": return new Color(245 / 255f, 222 / 255f, 179 / 255f);
                case "white": return new Color(255 / 255f, 255 / 255f, 255 / 255f);
                case "whitesmoke": return new Color(245 / 255f, 245 / 255f, 245 / 255f);
                case "yellow": return new Color(255 / 255f, 255 / 255f, 0 / 255f);
                case "yellowgreen": return new Color(154 / 255f, 205 / 255f, 50 / 255f);
                case "transparent":
                case "none": return new Color(0, 0, 0, 0);
                default: return null;
            }
        }
    }
}