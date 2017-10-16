using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Utility;

namespace Ibinimator.Core.Utility
{
    public static class ColorUtils
    {
        public static Color HslaToColor(double h, double s, double l, double alpha)
        {
            (double r, double g, double b) = HslToRgb(h, s, l);
            return RgbaToColor(r, g, b, alpha);
        }

        public static Color HslToColor(double h, double s, double l)
        {
            (double r, double g, double b) = HslToRgb(h, s, l);
            return RgbToColor(r, g, b);
        }

        public static (double r, double g, double b) HslToRgb(double h, double s, double l)
        {
            var c = (1 - Math.Abs(2 * l - 1)) * s; // chroma
            var x = c * (1 - Math.Abs(h / 60f % 2 - 1));
            var m = l - c / 2;

            (double r, double g, double b) = (0, 0, 0);

            if (h < 60) (r, g, b) = (c + m, x + m, m);
            else if (h < 120) (r, g, b) = (x + m, c + m, m);
            else if (h < 180) (r, g, b) = (m, c + m, x + m);
            else if (h < 240) (r, g, b) = (m, x + m, c + m);
            else if (h < 300) (r, g, b) = (x + m, m, c + m);
            else (r, g, b) = (c + m, m, x + m);

            return (r, g, b);
        }

        public static Color RgbaToColor(double r, double g, double b, double a)
        {
            return new Color
            {
                A = (float) a,
                R = (float) r,
                G = (float) g,
                B = (float) b
            };
        }

        public static (double h, double s, double l, double a) RgbaToHsla(double r, double g, double b, double a)
        {
            double max = MathUtils.Max(r, g, b), min = MathUtils.Min(r, g, b);
            double h, s, l = (max + min) / 2;

            if (Math.Abs(max - min) < double.Epsilon)
            {
                h = s = 0; // achromatic
            }
            else
            {
                var d = max - min;
                s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
                if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
                else if (max == g) h = (b - r) / d + 2;
                else h = (r - g) / d + 4;

                h /= 6;
            }

            return (h * 360f, s, l, a);
        }

        public static Color RgbToColor(double r, double g, double b)
        {
            return RgbaToColor(r, g, b, 1);
        }

        public static (double h, double s, double l) RgbToHsl(double r, double g, double b)
        {
            double max = MathUtils.Max(r, g, b), min = MathUtils.Min(r, g, b);
            double h, s, l = (max + min) / 2;

            if (max == min)
            {
                h = s = 0; // achromatic
            }
            else
            {
                var d = max - min;
                s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
                if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
                else if (max == g) h = (b - r) / d + 2;
                else h = (r - g) / d + 4;

                h /= 6;
            }

            return (h * 360f, s, l);
        }

        public static (double r, double g, double b, double a) ColorToRgba(Color color)
        {
            return (color.R, color.G, color.B, color.A);
        }

        public static (double hue, double saturation, double lightness, double alpha) ColorToHsla(Color color)
        {
            var (r, g, b, a) = ColorToRgba(color);
            return RgbaToHsla(r, g, b, a);
        }
    }
}