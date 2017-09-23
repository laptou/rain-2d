using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public class SvgSerializer : ISvgWriter, ISvgReader
    {
        public static Document Parse(XDocument document)
        {
            var doc = new Document();
            doc.FromXml(document.Root, null);
            return doc;
        }

        #region ISvgReader Members

        public Document Read(XDocument document)
        {
            return Parse(document);
        }

        #endregion

        #region ISvgWriter Members

        public XDocument Save(Document document)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}