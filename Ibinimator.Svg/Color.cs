using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Regex = System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ibinimator.Svg
{
    public struct Color
    {
        private static readonly Regex.Regex Hex = new Regex.Regex("(?:#(?:([0-9A-F]){3}){1,2})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex.Regex Rgb = new Regex.Regex(
            @"(?:rgb\(([+-]?[0-9]+)[\u0009\u000D\u000A]*,[\u0020\u0009\u000D\u000A]*([+-]?[0-9]+)[\u0020\u0009\u000D\u000A]*,[\u0020\u0009\u000D\u000A]*([+-]?[0-9]+)\))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex.Regex Percent = new Regex.Regex(
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
                var values = rgbMatch.Groups.OfType<Regex.Group>().Skip(1)
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

        public static bool TryParse(string input, out Color color)
        {
            color = new Color();

            if (string.IsNullOrWhiteSpace(input)) return false;

            var hexMatch = Hex.Match(input);

            if (hexMatch.Success)
            {
                var digits = hexMatch.Groups[1].Captures.OfType<Capture>()
                    .Select(c => c.Value)
                    .ToArray();

                if (digits.Length == 3)
                {
                    color = new Color(
                        Convert.ToInt32(digits[0] + digits[0], 16) / 256f,
                        Convert.ToInt32(digits[1] + digits[1], 16) / 256f,
                        Convert.ToInt32(digits[2] + digits[2], 16) / 256f);
                    return true;
                }

                if (digits.Length == 6)
                {
                    color = new Color(
                        Convert.ToInt32(digits[0] + digits[1], 16) / 256f,
                        Convert.ToInt32(digits[2] + digits[3], 16) / 256f,
                        Convert.ToInt32(digits[4] + digits[5], 16) / 256f);
                    return true;
                }
            }

            var rgbMatch = Rgb.Match(input);

            if (rgbMatch.Success)
            {
                var values = rgbMatch.Groups.OfType<Regex.Group>().Skip(1)
                    .Select(c => c.Value)
                    .ToArray();

                color = new Color(
                    int.Parse(values[0]) / 256f,
                    int.Parse(values[1]) / 256f,
                    int.Parse(values[2]) / 256f);
                return true;
            }

            var percentMatch = Percent.Match(input);

            if (percentMatch.Success)
            {
                var values = percentMatch.Groups
                    .OfType<System.Text.RegularExpressions.Group>()
                    .Skip(1)
                    .Select(g => g.Value)
                    .ToArray();

                color = new Color(
                    int.Parse(values[0]) / 100f,
                    int.Parse(values[1]) / 100f,
                    int.Parse(values[2]) / 100f);
                return true;
            }

            switch (input.ToLowerInvariant())
            {
                case "aliceblue": color = new Color(240 / 255f, 248 / 255f, 255 / 255f); return true;
                case "antiquewhite": color = new Color(250 / 255f, 235 / 255f, 215 / 255f); return true;
                case "aqua": color = new Color(0 / 255f, 255 / 255f, 255 / 255f); return true;
                case "aquamarine": color = new Color(127 / 255f, 255 / 255f, 212 / 255f); return true;
                case "azure": color = new Color(240 / 255f, 255 / 255f, 255 / 255f); return true;
                case "beige": color = new Color(245 / 255f, 245 / 255f, 220 / 255f); return true;
                case "bisque": color = new Color(255 / 255f, 228 / 255f, 196 / 255f); return true;
                case "black": color = new Color(0 / 255f, 0 / 255f, 0 / 255f); return true;
                case "blanchedalmond": color = new Color(255 / 255f, 235 / 255f, 205 / 255f); return true;
                case "blue": color = new Color(0 / 255f, 0 / 255f, 255 / 255f); return true;
                case "blueviolet": color = new Color(138 / 255f, 43 / 255f, 226 / 255f); return true;
                case "brown": color = new Color(165 / 255f, 42 / 255f, 42 / 255f); return true;
                case "burlywood": color = new Color(222 / 255f, 184 / 255f, 135 / 255f); return true;
                case "cadetblue": color = new Color(95 / 255f, 158 / 255f, 160 / 255f); return true;
                case "chartreuse": color = new Color(127 / 255f, 255 / 255f, 0 / 255f); return true;
                case "chocolate": color = new Color(210 / 255f, 105 / 255f, 30 / 255f); return true;
                case "coral": color = new Color(255 / 255f, 127 / 255f, 80 / 255f); return true;
                case "cornflowerblue": color = new Color(100 / 255f, 149 / 255f, 237 / 255f); return true;
                case "cornsilk": color = new Color(255 / 255f, 248 / 255f, 220 / 255f); return true;
                case "crimson": color = new Color(220 / 255f, 20 / 255f, 60 / 255f); return true;
                case "cyan": color = new Color(0 / 255f, 255 / 255f, 255 / 255f); return true;
                case "darkblue": color = new Color(0 / 255f, 0 / 255f, 139 / 255f); return true;
                case "darkcyan": color = new Color(0 / 255f, 139 / 255f, 139 / 255f); return true;
                case "darkgoldenrod": color = new Color(184 / 255f, 134 / 255f, 11 / 255f); return true;
                case "darkgray": color = new Color(169 / 255f, 169 / 255f, 169 / 255f); return true;
                case "darkgreen": color = new Color(0 / 255f, 100 / 255f, 0 / 255f); return true;
                case "darkgrey": color = new Color(169 / 255f, 169 / 255f, 169 / 255f); return true;
                case "darkkhaki": color = new Color(189 / 255f, 183 / 255f, 107 / 255f); return true;
                case "darkmagenta": color = new Color(139 / 255f, 0 / 255f, 139 / 255f); return true;
                case "darkolivegreen": color = new Color(85 / 255f, 107 / 255f, 47 / 255f); return true;
                case "darkorange": color = new Color(255 / 255f, 140 / 255f, 0 / 255f); return true;
                case "darkorchid": color = new Color(153 / 255f, 50 / 255f, 204 / 255f); return true;
                case "darkred": color = new Color(139 / 255f, 0 / 255f, 0 / 255f); return true;
                case "darksalmon": color = new Color(233 / 255f, 150 / 255f, 122 / 255f); return true;
                case "darkseagreen": color = new Color(143 / 255f, 188 / 255f, 143 / 255f); return true;
                case "darkslateblue": color = new Color(72 / 255f, 61 / 255f, 139 / 255f); return true;
                case "darkslategray": color = new Color(47 / 255f, 79 / 255f, 79 / 255f); return true;
                case "darkslategrey": color = new Color(47 / 255f, 79 / 255f, 79 / 255f); return true;
                case "darkturquoise": color = new Color(0 / 255f, 206 / 255f, 209 / 255f); return true;
                case "darkviolet": color = new Color(148 / 255f, 0 / 255f, 211 / 255f); return true;
                case "deeppink": color = new Color(255 / 255f, 20 / 255f, 147 / 255f); return true;
                case "deepskyblue": color = new Color(0 / 255f, 191 / 255f, 255 / 255f); return true;
                case "dimgray": color = new Color(105 / 255f, 105 / 255f, 105 / 255f); return true;
                case "dimgrey": color = new Color(105 / 255f, 105 / 255f, 105 / 255f); return true;
                case "dodgerblue": color = new Color(30 / 255f, 144 / 255f, 255 / 255f); return true;
                case "firebrick": color = new Color(178 / 255f, 34 / 255f, 34 / 255f); return true;
                case "floralwhite": color = new Color(255 / 255f, 250 / 255f, 240 / 255f); return true;
                case "forestgreen": color = new Color(34 / 255f, 139 / 255f, 34 / 255f); return true;
                case "fuchsia": color = new Color(255 / 255f, 0 / 255f, 255 / 255f); return true;
                case "gainsboro": color = new Color(220 / 255f, 220 / 255f, 220 / 255f); return true;
                case "ghostwhite": color = new Color(248 / 255f, 248 / 255f, 255 / 255f); return true;
                case "gold": color = new Color(255 / 255f, 215 / 255f, 0 / 255f); return true;
                case "goldenrod": color = new Color(218 / 255f, 165 / 255f, 32 / 255f); return true;
                case "gray": color = new Color(128 / 255f, 128 / 255f, 128 / 255f); return true;
                case "grey": color = new Color(128 / 255f, 128 / 255f, 128 / 255f); return true;
                case "green": color = new Color(0 / 255f, 128 / 255f, 0 / 255f); return true;
                case "greenyellow": color = new Color(173 / 255f, 255 / 255f, 47 / 255f); return true;
                case "honeydew": color = new Color(240 / 255f, 255 / 255f, 240 / 255f); return true;
                case "hotpink": color = new Color(255 / 255f, 105 / 255f, 180 / 255f); return true;
                case "indianred": color = new Color(205 / 255f, 92 / 255f, 92 / 255f); return true;
                case "indigo": color = new Color(75 / 255f, 0 / 255f, 130 / 255f); return true;
                case "ivory": color = new Color(255 / 255f, 255 / 255f, 240 / 255f); return true;
                case "khaki": color = new Color(240 / 255f, 230 / 255f, 140 / 255f); return true;
                case "lavender": color = new Color(230 / 255f, 230 / 255f, 250 / 255f); return true;
                case "lavenderblush": color = new Color(255 / 255f, 240 / 255f, 245 / 255f); return true;
                case "lawngreen": color = new Color(124 / 255f, 252 / 255f, 0 / 255f); return true;
                case "lemonchiffon": color = new Color(255 / 255f, 250 / 255f, 205 / 255f); return true;
                case "lightblue": color = new Color(173 / 255f, 216 / 255f, 230 / 255f); return true;
                case "lightcoral": color = new Color(240 / 255f, 128 / 255f, 128 / 255f); return true;
                case "lightcyan": color = new Color(224 / 255f, 255 / 255f, 255 / 255f); return true;
                case "lightgoldenrodyellow": color = new Color(250 / 255f, 250 / 255f, 210 / 255f); return true;
                case "lightgray": color = new Color(211 / 255f, 211 / 255f, 211 / 255f); return true;
                case "lightgreen": color = new Color(144 / 255f, 238 / 255f, 144 / 255f); return true;
                case "lightgrey": color = new Color(211 / 255f, 211 / 255f, 211 / 255f); return true;
                case "lightpink": color = new Color(255 / 255f, 182 / 255f, 193 / 255f); return true;
                case "lightsalmon": color = new Color(255 / 255f, 160 / 255f, 122 / 255f); return true;
                case "lightseagreen": color = new Color(32 / 255f, 178 / 255f, 170 / 255f); return true;
                case "lightskyblue": color = new Color(135 / 255f, 206 / 255f, 250 / 255f); return true;
                case "lightslategray": color = new Color(119 / 255f, 136 / 255f, 153 / 255f); return true;
                case "lightslategrey": color = new Color(119 / 255f, 136 / 255f, 153 / 255f); return true;
                case "lightsteelblue": color = new Color(176 / 255f, 196 / 255f, 222 / 255f); return true;
                case "lightyellow": color = new Color(255 / 255f, 255 / 255f, 224 / 255f); return true;
                case "lime": color = new Color(0 / 255f, 255 / 255f, 0 / 255f); return true;
                case "limegreen": color = new Color(50 / 255f, 205 / 255f, 50 / 255f); return true;
                case "linen": color = new Color(250 / 255f, 240 / 255f, 230 / 255f); return true;
                case "magenta": color = new Color(255 / 255f, 0 / 255f, 255 / 255f); return true;
                case "maroon": color = new Color(128 / 255f, 0 / 255f, 0 / 255f); return true;
                case "mediumaquamarine": color = new Color(102 / 255f, 205 / 255f, 170 / 255f); return true;
                case "mediumblue": color = new Color(0 / 255f, 0 / 255f, 205 / 255f); return true;
                case "mediumorchid": color = new Color(186 / 255f, 85 / 255f, 211 / 255f); return true;
                case "mediumpurple": color = new Color(147 / 255f, 112 / 255f, 219 / 255f); return true;
                case "mediumseagreen": color = new Color(60 / 255f, 179 / 255f, 113 / 255f); return true;
                case "mediumslateblue": color = new Color(123 / 255f, 104 / 255f, 238 / 255f); return true;
                case "mediumspringgreen": color = new Color(0 / 255f, 250 / 255f, 154 / 255f); return true;
                case "mediumturquoise": color = new Color(72 / 255f, 209 / 255f, 204 / 255f); return true;
                case "mediumvioletred": color = new Color(199 / 255f, 21 / 255f, 133 / 255f); return true;
                case "midnightblue": color = new Color(25 / 255f, 25 / 255f, 112 / 255f); return true;
                case "mintcream": color = new Color(245 / 255f, 255 / 255f, 250 / 255f); return true;
                case "mistyrose": color = new Color(255 / 255f, 228 / 255f, 225 / 255f); return true;
                case "moccasin": color = new Color(255 / 255f, 228 / 255f, 181 / 255f); return true;
                case "navajowhite": color = new Color(255 / 255f, 222 / 255f, 173 / 255f); return true;
                case "navy": color = new Color(0 / 255f, 0 / 255f, 128 / 255f); return true;
                case "oldlace": color = new Color(253 / 255f, 245 / 255f, 230 / 255f); return true;
                case "olive": color = new Color(128 / 255f, 128 / 255f, 0 / 255f); return true;
                case "olivedrab": color = new Color(107 / 255f, 142 / 255f, 35 / 255f); return true;
                case "orange": color = new Color(255 / 255f, 165 / 255f, 0 / 255f); return true;
                case "orangered": color = new Color(255 / 255f, 69 / 255f, 0 / 255f); return true;
                case "orchid": color = new Color(218 / 255f, 112 / 255f, 214 / 255f); return true;
                case "palegoldenrod": color = new Color(238 / 255f, 232 / 255f, 170 / 255f); return true;
                case "palegreen": color = new Color(152 / 255f, 251 / 255f, 152 / 255f); return true;
                case "paleturquoise": color = new Color(175 / 255f, 238 / 255f, 238 / 255f); return true;
                case "palevioletred": color = new Color(219 / 255f, 112 / 255f, 147 / 255f); return true;
                case "papayawhip": color = new Color(255 / 255f, 239 / 255f, 213 / 255f); return true;
                case "peachpuff": color = new Color(255 / 255f, 218 / 255f, 185 / 255f); return true;
                case "peru": color = new Color(205 / 255f, 133 / 255f, 63 / 255f); return true;
                case "pink": color = new Color(255 / 255f, 192 / 255f, 203 / 255f); return true;
                case "plum": color = new Color(221 / 255f, 160 / 255f, 221 / 255f); return true;
                case "powderblue": color = new Color(176 / 255f, 224 / 255f, 230 / 255f); return true;
                case "purple": color = new Color(128 / 255f, 0 / 255f, 128 / 255f); return true;
                case "red": color = new Color(255 / 255f, 0 / 255f, 0 / 255f); return true;
                case "rosybrown": color = new Color(188 / 255f, 143 / 255f, 143 / 255f); return true;
                case "royalblue": color = new Color(65 / 255f, 105 / 255f, 225 / 255f); return true;
                case "saddlebrown": color = new Color(139 / 255f, 69 / 255f, 19 / 255f); return true;
                case "salmon": color = new Color(250 / 255f, 128 / 255f, 114 / 255f); return true;
                case "sandybrown": color = new Color(244 / 255f, 164 / 255f, 96 / 255f); return true;
                case "seagreen": color = new Color(46 / 255f, 139 / 255f, 87 / 255f); return true;
                case "seashell": color = new Color(255 / 255f, 245 / 255f, 238 / 255f); return true;
                case "sienna": color = new Color(160 / 255f, 82 / 255f, 45 / 255f); return true;
                case "silver": color = new Color(192 / 255f, 192 / 255f, 192 / 255f); return true;
                case "skyblue": color = new Color(135 / 255f, 206 / 255f, 235 / 255f); return true;
                case "slateblue": color = new Color(106 / 255f, 90 / 255f, 205 / 255f); return true;
                case "slategray": color = new Color(112 / 255f, 128 / 255f, 144 / 255f); return true;
                case "slategrey": color = new Color(112 / 255f, 128 / 255f, 144 / 255f); return true;
                case "snow": color = new Color(255 / 255f, 250 / 255f, 250 / 255f); return true;
                case "springgreen": color = new Color(0 / 255f, 255 / 255f, 127 / 255f); return true;
                case "steelblue": color = new Color(70 / 255f, 130 / 255f, 180 / 255f); return true;
                case "tan": color = new Color(210 / 255f, 180 / 255f, 140 / 255f); return true;
                case "teal": color = new Color(0 / 255f, 128 / 255f, 128 / 255f); return true;
                case "thistle": color = new Color(216 / 255f, 191 / 255f, 216 / 255f); return true;
                case "tomato": color = new Color(255 / 255f, 99 / 255f, 71 / 255f); return true;
                case "turquoise": color = new Color(64 / 255f, 224 / 255f, 208 / 255f); return true;
                case "violet": color = new Color(238 / 255f, 130 / 255f, 238 / 255f); return true;
                case "wheat": color = new Color(245 / 255f, 222 / 255f, 179 / 255f); return true;
                case "white": color = new Color(255 / 255f, 255 / 255f, 255 / 255f); return true;
                case "whitesmoke": color = new Color(245 / 255f, 245 / 255f, 245 / 255f); return true;
                case "yellow": color = new Color(255 / 255f, 255 / 255f, 0 / 255f); return true;
                case "yellowgreen": color = new Color(154 / 255f, 205 / 255f, 50 / 255f); return true;
                case "transparent":
                case "none": color = new Color(0, 0, 0, 0); return true;
                default: return false;
            }
        }

        public override string ToString()
        {
            return $"rgb({Red * 100}%, {Green * 100}%, {Blue * 100}%)";
        }
    }
}