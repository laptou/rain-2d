using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Xml.Linq;

using Rain.Core.Model.Paint;
using Rain.Formatter.Svg.Utilities;

namespace Rain.Formatter.Svg.Paint
{
    public abstract class GradientPaint : ReferencePaint
    {
        /// <inheritdoc />
        public override Iri Reference
        {
            get => Iri.FromId(Id);
            set => throw new InvalidOperationException();
        }

        public GradientSpace Space { get; set; }

        public SpreadMethod SpreadMethod { get; set; } = SpreadMethod.Pad;

        public GradientStop[] Stops { get; set; }

        public Matrix3x2 Transform { get; set; } = Matrix3x2.Identity;

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            if (Iri.TryParse(LazyGet(element, SvgNames.XLink + "href"), out var href))
                FromXml(context.GetXmlElementByIri(href), context);

            if (Stops == null)
                Stops = element.Elements(SvgNames.Stop)
                               .Select(x =>
                                       {
                                           var stop = new GradientStop();
                                           stop.FromXml(x, context);

                                           return stop;
                                       })
                               .ToArray();

            var space = LazyGet(element, "gradientUnits", "objectBoundingBox");

            switch (space)
            {
                case "userSpaceOnUse":
                    Space = GradientSpace.Absolute;

                    break;

                default:
                    Space = GradientSpace.Relative;

                    break;
            }

            Transform = X.ParseTransform(LazyGet(element, "gradientTransform"));
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            var space = "objectBoundingBox";

            switch (Space)
            {
                case GradientSpace.Absolute:
                    space = "userSpaceOnUse";

                    break;
            }

            LazySet(element, "gradientUnits", space);

            element.Add(Stops.Select(g => g.ToXml(context)));

            return element;
        }
    }
}