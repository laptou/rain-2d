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
        public float Version { get; set; }

        public Length Height { get; set; }

        public Length Width { get; set; }

        public RectangleF Viewbox { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            Version = float.Parse((string)element.Attribute("version"));
            Width = Length.Parse((string)element.Attribute("width"));
            Height = Length.Parse((string)element.Attribute("height"));
        }
    }
}