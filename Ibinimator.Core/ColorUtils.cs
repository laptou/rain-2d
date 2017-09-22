using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Mathematics;

namespace Ibinimator.Core
{
    public static class ColorUtils
    {
        public static Color HslaToColor(float h, float s, float l, float a)
        {
            (float r, float g, float b) = HslToRgb(h, s, l);
            return new Color(r, g, b, a);
        }

        public static Color HslToColor(float h, float s, float l)
        {
            (float r, float g, float b) = HslToRgb(h, s, l);
            return RgbToColor(r, g, b);
        }

        public static (float r, float g, float b) HslToRgb(float h, float s, float l)
        {
            var c = (1 - Math.Abs(2 * l - 1)) * s; // chroma
            var x = c * (1 - Math.Abs(h / 60f % 2 - 1));
            var m = l - c / 2;

            (float r, float g, float b) = (0, 0, 0);

            if (h < 60) (r, g, b) = (c + m, x + m, m);
            else if (h < 120) (r, g, b) = (x + m, c + m, m);
            else if (h < 180) (r, g, b) = (m, c + m, x + m);
            else if (h < 240) (r, g, b) = (m, x + m, c + m);
            else if (h < 300) (r, g, b) = (x + m, m, c + m);
            else (r, g, b) = (c + m, m, x + m);

            return (r, g, b);
        }

        public static (float h, float s, float l, float a) RgbaToHsla(float r, float g, float b, float a)
        {
            float max = MathUtils.Max(r, g, b), min = MathUtils.Min(r, g, b);
            float h, s, l = (max + min) / 2;

            if (Math.Abs(max - min) < MathUtils.Epsilon)
            {
                h = s = 0; // achromatic
            }
            else
            {
                var d = max - min;
                s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
                if (Math.Abs(max - r) < MathUtils.Epsilon) h = (g - b) / d + (g < b ? 6 : 0);
                else if (Math.Abs(max - g) < MathUtils.Epsilon) h = (b - r) / d + 2;
                else h = (r - g) / d + 4;

                h /= 6;
            }

            return (h * 360f, s, l, a);
        }

        public static (float h, float s, float l) RgbToHsl(float r, float g, float b)
        {
            var (h, s, l, _) = RgbaToHsla(r, g, b, 1);
            return (h, s, l);
        }
    }
}