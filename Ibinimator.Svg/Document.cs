using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public class Document : ContainerElement
    {
        public Length Height { get; set; }
        public float Version { get; set; }

        public RectangleF Viewbox { get; set; }

        public Length Width { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
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
    }
}