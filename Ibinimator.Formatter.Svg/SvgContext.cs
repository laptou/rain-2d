using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Ibinimator.Formatter.Svg.Structure;
using Ibinimator.Formatter.Svg.Utilities;

namespace Ibinimator.Formatter.Svg
{
    public sealed class SvgContext
    {
        private readonly Dictionary<string, IElement>
            _elements = new Dictionary<string, IElement>();

        public IElement this[string id]
        {
            get =>
                _elements.TryGetValue(id, out var element)
                    ? element
                    : _elements[id] = GetElementById(id);

            set => _elements[id] = value;
        }

        public XElement Root { get; set; }

        public IElement GetElementById(string id) { return X.FromXml(GetXmlElementById(id), this); }

        public IElement GetElementByIri(Iri iri) { return GetElementById(iri.Id); }

        public XElement GetXmlElementById(string id)
        {
            return Root.DescendantsAndSelf().FirstOrDefault(x => (string) x.Attribute("id") == id);
        }

        public XElement GetXmlElementByIri(Iri iri) { return GetXmlElementById(iri.Id); }
    }
}