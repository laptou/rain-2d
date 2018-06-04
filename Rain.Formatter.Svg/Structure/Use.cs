using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Rain.Formatter.Svg.Shapes;
using Rain.Formatter.Svg.Utilities;

namespace Rain.Formatter.Svg.Structure
{
    public class Use : ShapeElementBase
    {
        public Uri Target { get; set; }

        /// <inheritdoc />
        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            if (UriHelper.TryParse(LazyGet(element, SvgNames.HRef), out var target))
                Target = target;
        }

        /// <inheritdoc />
        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Use;

            LazySet(element, SvgNames.HRef, Target);

            return element;
        }
    }
}