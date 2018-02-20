using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Formatter.Svg.Shapes
{
    public class Span : TextElement, IInlineTextElement
    {
        #region IInlineTextElement Members

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Tspan;

            return element;
        }

        public int Position { get; set; }

        #endregion
    }
}