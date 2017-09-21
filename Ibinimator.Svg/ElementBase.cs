using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
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

            var style = (string) element.Attribute("style");

            if (style != null)
            {
                var rules =
                    from Match match in Regex.Matches(style, @"([a-z-]+):([a-zA-Z0-9""'#\(\)\.\, ]+);?")
                    select (name: match.Groups[1].Value, value: match.Groups[2].Value);

                foreach (var rule in rules)
                    if(element.Attribute(rule.name) == null)
                        element.SetAttributeValue(rule.name, rule.value);
            }
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