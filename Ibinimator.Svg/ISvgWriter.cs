using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public interface ISvgWriter
    {
        XDocument Save(Document document);
    }

    
}
