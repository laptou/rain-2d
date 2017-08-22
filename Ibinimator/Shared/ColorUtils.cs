using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpDX;
using Color = System.Windows.Media.Color;

namespace Ibinimator.Shared
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
                A = (byte) (a * 255f),
                R = (byte) (r * 255f),
                G = (byte) (g * 255f),
                B = (byte) (b * 255f)
            };
        }

        public static (double h, double s, double l, double a) RgbaToHsla(double r, double g, double b, double a)
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

            return (h * 360f, s, l, a);
        }

        public static Color RgbToColor(double r, double g, double b)
        {
            return new Color
            {
                A = 255,
                R = (byte) (r * 255f),
                G = (byte) (g * 255f),
                B = (byte) (b * 255f)
            };
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

        public static Color4 ToDirectX(this Color color)
        {
            return new Color4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }

        public static Color ToWpf(this Color4 color)
        {
            return RgbaToColor(color.Red, color.Green, color.Blue, color.Alpha);
        }
    }
}