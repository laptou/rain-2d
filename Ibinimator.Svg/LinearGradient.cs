using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml.Linq;
using Ibinimator.Core.Model;

namespace Ibinimator.Svg
{
    public abstract class GradientPaint : Paint
    {
        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);


            if (Iri.TryParse(LazyGet(element, SvgNames.XLink + "href"), out var href))
            {
                var target = context.Root.DescendantsAndSelf().First(x => (string)x.Attribute("id") == href.Id);
                FromXml(target, context);
            }

            if(Stops == null)
                Stops = element.Elements(SvgNames.Stop).Select(x =>
                {
                    var stop = new GradientStop();
                    stop.FromXml(x, context);
                    return stop;
                }).ToArray();
            
            
            Transform = X.ParseTransform(LazyGet(element, "gradientTransform"));
        }

        public GradientStop[] Stops { get; set; }

        public Matrix3x2 Transform { get; set; } = Matrix3x2.Identity;

        public SpreadMethod SpreadMethod { get; set; } = SpreadMethod.Pad;
    }

    public enum SpreadMethod
    {
        Pad,
        Reflect,
        Repeat
    }

    public class GradientStop : Element
    {
        public Length Offset { get; set; }

        public Color Color { get; set; }

        public float Opacity { get; set; } = 1;

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            Offset = LazyGet(element, "offset", Length.Zero);
            Opacity = LazyGet(element, "stop-opacity", 1f, true);
            Color = LazyGet<Color>(element, "stop-color", inherit: true);
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Stop;

            LazySet(element, "offset", Offset);
            LazySet(element, "stop-opacity", Opacity);
            LazySet(element, "stop-color", Color);

            return element;
        }
    }

    public class LinearGradient : GradientPaint
    {
        public Length X1 { get; set; }
        public Length X2 { get; set; } = (100, LengthUnit.Percent);
        public Length Y1 { get; set; }
        public Length Y2 { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            X1 = LazyGet(element, "x1", Length.Zero);
            Y1 = LazyGet(element, "y1", Length.Zero);
            X2 = LazyGet(element, "x2", Length.Zero);
            Y2 = LazyGet(element, "y2", Length.Zero);
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.LinearGradient;

            LazySet(element, "x1", X1);
            LazySet(element, "y1", Y1);
            LazySet(element, "x2", X2);
            LazySet(element, "y2", Y2);

            return element;
        }
    }

    public class RadialGradient : GradientPaint
    {
        public Length CenterX { get; set; }
        public Length CenterY { get; set; }
        public Length FocusX { get; set; }
        public Length FocusY { get; set; }
        public Length Radius { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            CenterX = LazyGet(element, "cx", Length.Zero);
            CenterY = LazyGet(element, "cy", Length.Zero);
            CenterY = LazyGet(element, "fx", Length.Zero);
            CenterY = LazyGet(element, "fy", Length.Zero);
            Radius = LazyGet(element, "r", Length.Zero);
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.RadialGradient;

            LazySet(element, "cx", CenterX);
            LazySet(element, "cy", CenterY);
            LazySet(element, "fx", FocusX, CenterX);
            LazySet(element, "fy", FocusY, CenterY);
            LazySet(element, "r", Radius);

            return element;
        }
}
}
