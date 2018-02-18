using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Ibinimator.Svg.Structure;
using Ibinimator.Svg.Utilities;

namespace Ibinimator.Svg
{
    public sealed class SvgContext
    {
        private readonly Dictionary<string, IElement> _elements = new Dictionary<string, IElement>();

        public IElement this[string id]
        {
            get =>
                _elements.TryGetValue(id, out var element) ?
                    element : _elements[id] = GetElementById(id);

            set => _elements[id] = value;
        }

        public IElement GetElementByIri(Iri iri) { return GetElementById(iri.Id); }
        public XElement GetXmlElementByIri(Iri iri) { return GetXmlElementById(iri.Id); }

        public IElement GetElementById(string id) { return X.FromXml(GetXmlElementById(id), this); }

        public XElement GetXmlElementById(string id)
        {
            return Root.DescendantsAndSelf().FirstOrDefault(x => (string) x.Attribute("id") == id);
        }

        public XElement Root { get; set; }
    }
}