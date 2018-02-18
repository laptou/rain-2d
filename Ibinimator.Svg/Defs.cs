using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Ibinimator.Svg.Structure;

namespace Ibinimator.Svg {
    public class Defs : ContainerElement
    {
        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Defs;

            return element;
        }
    }
}