using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Measurement;
using Rain.Core.Model.Paint;

namespace Rain.Formatter.Svg.Paint
{
    public static class BrushConverter
    {
        public static Paint Convert(this IBrushInfo brush)
        {
            switch (brush)
            {
                case GradientBrushInfo gradient:

                    return gradient.Convert();

                case SolidColorBrushInfo solid:

                    return solid.Convert();
            }

            return null;
        }

        public static SolidColorPaint Convert(this SolidColorBrushInfo brush)
        {
            SolidColorPaint paint;

            if (brush.Scope == ResourceScope.Document)
                paint = new SolidColorPaint(brush.Name, brush.Color);
            else
                paint = new SolidColorPaint(null, brush.Color);

            return paint;
        }

        public static GradientPaint Convert(this GradientBrushInfo brush)
        {
            GradientPaint paint = null;

            switch (brush.Type)
            {
                case GradientBrushType.Linear:
                    paint = new LinearGradientPaint(brush.Name,
                                                    brush.Stops.Select(s => new GradientStop
                                                    {
                                                        Color = s.Color,
                                                        Offset = (s.Offset * 100, LengthUnit.Percent)
                                                    }))
                    {
                        X1 = (brush.StartPoint.X, LengthUnit.Pixels),
                        Y1 = (brush.StartPoint.Y, LengthUnit.Pixels),
                        X2 = (brush.EndPoint.X, LengthUnit.Pixels),
                        Y2 = (brush.EndPoint.Y, LengthUnit.Pixels)
                    };

                    break;
                case GradientBrushType.Radial:
                    var radii = brush.EndPoint - brush.StartPoint;
                    var focus = brush.FocusOffset + brush.StartPoint;

                    paint = new RadialGradientPaint(brush.Name,
                                                    brush.Stops.Select(s => new GradientStop
                                                    {
                                                        Color = s.Color,
                                                        Offset = (s.Offset * 100, LengthUnit.Percent)
                                                    }))
                    {
                        CenterX = (brush.StartPoint.X, LengthUnit.Pixels),
                        CenterY = (brush.StartPoint.Y, LengthUnit.Pixels),
                        FocusX = (focus.X, LengthUnit.Pixels),
                        FocusY = (focus.Y, LengthUnit.Pixels),
                        Radius = (radii.X, LengthUnit.Pixels),
                        Transform = Matrix3x2.CreateScale(1, radii.Y / radii.X, brush.StartPoint)
                    };
                    break;
            }

            if (paint != null)
            {
                paint.Space = brush.Space;
                paint.Opacity = brush.Opacity;
            }

            return paint;
        }
    }
}