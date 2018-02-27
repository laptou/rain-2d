using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Rain.Formatter.Svg.Shapes
{
    public class Text : TextElementBase
    {
        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Text;

            return element;
        }
    }
}