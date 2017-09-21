using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public class SvgSerializer : ISvgWriter, ISvgReader
    {
        public XDocument Save(SvgDocument document)
        {
            throw new NotImplementedException();
        }

        public SvgDocument Read(XDocument document)
        {
            return Parse(document);
        }

        public static SvgDocument Parse(XDocument document)
        {
            var doc = new Document();
            doc.FromXml(document.Root, null);
            return doc;
        }
    }
}