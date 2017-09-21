using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg.Reader
{
    public abstract class ElementBase : IElement
    {
        public string Id { get; set; }

        public virtual XElement ToXml(SvgContext context)
        {
            var element = new XElement("");

            LazySet(element, "id", Id);

            return element;
        }

        public virtual void FromXml(XElement element, SvgContext context)
        {
            Id = (string)element.Attribute("id");
        }

        protected void LazySet<T>(XElement element, XName name, T value)
        {
            LazySet(element, name, value, default);
        }

        protected void LazySet<T>(XElement element, XName name, T value, T @default)
        {
            if (value?.Equals(@default) == false)
            {
                if(value is object[] array)
                    element.SetAttributeValue(name, string.Join(",", array));
                else element.SetAttributeValue(name, value);
            }
        }
    }
}