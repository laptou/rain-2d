using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public interface IElement
    {
        string Id { get; set; }
        void FromXml(XElement element, SvgContext context);

        XElement ToXml(SvgContext context);
    }
}