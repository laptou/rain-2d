using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg.Reader
{
    public interface IElement
    {
        string Id { get; set; }

        XElement ToXml(SvgContext context);
        void FromXml(XElement element, SvgContext context);
    }
}