using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Rain.Core.Model;
using Rain.Core.Model.Measurement;

namespace Rain.Formatter.Svg.Structure
{
    public class Document : GraphicalContainerElementBase
    {
        public Defs Defs => (Defs) this.FirstOrDefault(c => c is Defs);

        public Length Height { get; set; } = (100, LengthUnit.Percent);

        public float Version { get; set; } = 1.1f;

        public RectangleF Viewbox { get; set; }

        public Length Width { get; set; } = (100, LengthUnit.Percent);

        public override void FromXml(XElement element, SvgContext context)
        {
            context.Root = element;

            base.FromXml(element, context);

            if ((string) element.Attribute("viewBox") != null)
            {
                var vb = ((string) element.Attribute("viewBox"))
                        .Split(' ')
                        .Select(float.Parse)
                        .ToArray();

                Viewbox = new RectangleF(vb[0], vb[1], vb[2], vb[3]);
            }

            if (float.TryParse(LazyGet(element, "version"), out var version))
                Version = version;

            if (Length.TryParse(LazyGet(element, "width"), out var width))
                Width = width;

            if (Length.TryParse(LazyGet(element, "height"), out var height))
                Height = height;
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Svg;
            element.Add(
                new XAttribute(XNamespace.Xmlns + "rain", SvgNames.Rain2D.NamespaceName));

            LazySet(element, "viewBox", Viewbox);
            LazySet(element, "version", Version);
            LazySet(element, "width", Width);
            LazySet(element, "height", Height);

            return element;
        }
    }
}