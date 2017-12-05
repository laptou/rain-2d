using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Ibinimator.Core.Model;

namespace Ibinimator.Svg
{
    public abstract class Element : IElement
    {
        protected string LazyGet(XElement element, XName name, bool inherit = false)
        {
            if(inherit)
                return (string)element.AncestorsAndSelf()
                                      .Select(x => x.Attribute(name))
                                      .FirstOrDefault(a => a != null);

            return (string) element.Attribute(name);
        }

        protected T LazyGet<T>(XElement element, XName name, T @default = default, bool inherit = false)
        {
            var value = LazyGet(element, name, inherit);

            if (typeof(T).IsEnum)
            {
                if (string.IsNullOrWhiteSpace(value)) return @default;

                var pascalCase = string.Join("", value.Split('-').Select(s => char.ToUpper(s[0]) + s.Substring(1)));

                return (T) Enum.Parse(typeof(T), pascalCase);
            }

            if (typeof(T) == typeof(float[]))
            {
                if (string.IsNullOrWhiteSpace(value)) return (T)(object)new float[0];

                return (T)(object)value.Split(',', ' ')
                                       .Where(s => float.TryParse(s, out var _))
                                       .Select(float.Parse).ToArray();
            }

            if (typeof(T) == typeof(float))
            {
                if (value != null && float.TryParse(value, out var result))
                    return (T)(object)result;
            }

            if (typeof(T) == typeof(Length))
            {
                if (value != null && Length.TryParse(value, out var result))
                    return (T)(object)result;
            }

            if (typeof(T) == typeof(Color))
            {
                if (value != null && Color.TryParse(value, out var result))
                    return (T)(object)result;
            }

            if (typeof(T) == typeof(Matrix3x2))
            {
                if (value != null)
                {
                    var values = value.Split('\u0020', '\u000A', '\u000D', '\u0009')
                        .Select(s => float.TryParse(s, out var result) ? result : float.NaN)
                        .ToArray();

                    return (T)(object)new Matrix3x2(
                        values[0], values[1], 
                        values[2], values[3], 
                        values[4], values[5]);
                }
            }

            if (typeof(T) == typeof(string))
                return (T)(object)value;

            return @default;
        }

        protected void LazySet<T>(XElement element, XName name, T value)
        {
            LazySet(element, name, value, default);
        }

        protected void LazySet<T>(XElement element, XName name, T value, T @default)
        {
            if (value == null) return;
            if (Equals(value, @default)) return;

            switch (value)
            {
                case Array array:
                    if(array.Length > 0)
                        element.SetAttributeValue(name, string.Join(",", array.OfType<object>()));
                    break;
                case Enum @enum:
                    element.SetAttributeValue(name, @enum.Svgify());
                    break;
                case RectangleF rect:
                    element.SetAttributeValue(name, $"{rect.Left} {rect.Top} {rect.Width} {rect.Height}");
                    break;
                default:
                    element.SetAttributeValue(name, value);
                    break;
            }
        }

        #region IElement Members

        public virtual void FromXml(XElement element, SvgContext context)
        {

            Id = (string) element.Attribute("id");

            var style = (string) element.Attribute("style");

            if (style != null)
            {
                var rules =
                    from Match match in Regex.Matches(style, @"([a-z-]+):([a-zA-Z0-9""'#\(\)\.\, _]+);?")
                    select (name: match.Groups[1].Value, value: match.Groups[2].Value);

                foreach (var rule in rules)
                    if (element.Attribute(rule.name) == null)
                        element.SetAttributeValue(rule.name, rule.value);
            }

            if(Id != null)
                context[Id] = this;
        }

        public virtual XElement ToXml(SvgContext context)
        {
            var element = new XElement("error");

            LazySet(element, "id", Id);

            return element;
        }

        public string Id { get; set; }

        #endregion
    }
}