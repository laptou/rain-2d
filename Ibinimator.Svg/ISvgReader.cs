using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public interface ISvgReader
    {
        SvgDocument Read(XDocument document);
    }
}
