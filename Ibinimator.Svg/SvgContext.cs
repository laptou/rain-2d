using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public class SvgContext
    {
        private readonly Dictionary<string, IElement> _elements = new Dictionary<string, IElement>();

        public IElement this[string id]
        {
            get => 
                _elements.TryGetValue(id, out var element) ? 
                element :
                _elements[id] = 
                    X.FromXml(
                        Root.DescendantsAndSelf()
                            .FirstOrDefault(x => (string)x.Attribute("id") == id), 
                    this);

            set => _elements[id] = value;
        }

        public XElement Root { get; set; }
    }
}