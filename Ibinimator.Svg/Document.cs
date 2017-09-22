using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public class Document : ContainerElementBase
    {
        public Length Height { get; set; }
        public float Version { get; set; }

        public RectangleF Viewbox { get; set; }

        public Length Width { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            var vb = ((string) element.Attribute("viewBox"))
                .Split(' ')
                .Select(float.Parse)
                .ToArray();
            Viewbox = new RectangleF(vb[0], vb[1], vb[2], vb[3]);
            Version = float.Parse((string) element.Attribute("version"));
            Width = Length.Parse((string) element.Attribute("width"));
            Height = Length.Parse((string) element.Attribute("height"));
        }
    }
}