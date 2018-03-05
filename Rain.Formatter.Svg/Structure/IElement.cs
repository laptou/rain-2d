using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Rain.Formatter.Svg.Structure
{
    public interface IElement
    {
        string Id { get; set; }
        string Name { get; set; }
        IContainerElement Parent { get; set; }
        Document Document { get; }

        void FromXml(XElement element, SvgContext context);
        XElement ToXml(SvgContext context);
    }
}