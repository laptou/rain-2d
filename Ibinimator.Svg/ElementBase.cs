﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public abstract class ElementBase : IElement
    {

        protected string LazyGet(XElement element, XName name, bool inherit = false)
        {
            if(inherit)
                return (string)element.AncestorsAndSelf()
                                      .Select(x => x.Attribute(name))
                                      .FirstOrDefault(a => a != null);

            return (string) element.Attribute(name);
        }

        protected float LazyGet(XElement element, XName name, float @default)
        {
            var value = LazyGet(element, name);

            if (value != null && float.TryParse(value, out var result))
                return result;

            return @default;
        }

        protected Length LazyGet(XElement element, XName name, Length @default)
        {
            var value = LazyGet(element, name);

            if (value != null && Length.TryParse(value, out var result))
                return result;

            return @default;
        }

        protected Matrix3x2 LazyGet(XElement element, XName name, Matrix3x2 @default)
        {
            var value = LazyGet(element, name);

            if (value != null)
            {
                var values = value.Split('\u0020', '\u000A', '\u000D', '\u0009')
                    .Select(s => float.TryParse(s, out var result) ? result : float.NaN)
                    .ToArray();

                return new Matrix3x2(values[0], values[1], values[2], values[3], values[4], values[5]);
            }

            return @default;
        }

        protected T LazyGet<T>(XElement element, XName name, bool inherit = false)
        {
            var value = LazyGet(element, name, inherit);

            if (typeof(T).IsEnum)
            {
                if (string.IsNullOrWhiteSpace(value)) return default;

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

            return default;
        }

        protected void LazySet<T>(XElement element, XName name, T value)
        {
            LazySet(element, name, value, default);
        }

        protected void LazySet<T>(XElement element, XName name, T value, T @default)
        {
            if (value?.Equals(@default) == false)
                if (value is object[] array)
                    element.SetAttributeValue(name, string.Join(",", array));
                else element.SetAttributeValue(name, value);
        }

        #region IElement Members

        public virtual void FromXml(XElement element, SvgContext context)
        {
            Id = (string) element.Attribute("id");

            if(Id == "path4507") Debugger.Break();

            var style = (string) element.Attribute("style");

            if (style != null)
            {
                var rules =
                    from Match match in Regex.Matches(style, @"([a-z-]+):([a-zA-Z0-9""'#\(\)\.\, ]+);?")
                    select (name: match.Groups[1].Value, value: match.Groups[2].Value);

                foreach (var rule in rules)
                    if (element.Attribute(rule.name) == null)
                        element.SetAttributeValue(rule.name, rule.value);
            }
        }

        public virtual XElement ToXml(SvgContext context)
        {
            var element = new XElement("");

            LazySet(element, "id", Id);

            return element;
        }

        public string Id { get; set; }

        #endregion
    }
}