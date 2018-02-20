using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model;
using Ibinimator.Core.Model.DocumentGraph;
using Ibinimator.Core.Model.Measurement;
using Ibinimator.Core.Model.Paint;

namespace Ibinimator.Formatter.Svg.Paint
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

            if (brush.Type == GradientBrushType.Linear)
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
                    Y2 = (brush.EndPoint.X, LengthUnit.Pixels)
                };

            if (paint != null)
            {
                paint.Space = brush.Space;
                paint.Opacity = brush.Opacity;
            }

            return paint;
        }
    }
}