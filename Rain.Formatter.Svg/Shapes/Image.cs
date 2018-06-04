using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Rain.Core.Model.Measurement;
using Rain.Formatter.Svg.Structure;
using Rain.Formatter.Svg.Utilities;

namespace Rain.Formatter.Svg.Shapes
{
    public class Image : GraphicalElementBase, IImageElement
    {
        /// <inheritdoc />
        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            X = LazyGet(element, "x", Length.Zero);
            Y = LazyGet(element, "y", Length.Zero);
            Width = LazyGet(element, "width", Length.Zero);
            Height = LazyGet(element, "height", Length.Zero);

            if (UriHelper.TryParse(LazyGet(element, SvgNames.HRef), out var href))
                Href = href;
        }

        /// <inheritdoc />
        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Image;

            LazySet(element, "x", X, Length.Zero);
            LazySet(element, "y", Y, Length.Zero);
            LazySet(element, "height", Height, Length.Zero);
            LazySet(element, "width", Width, Length.Zero);
            LazySet(element, SvgNames.HRef, Href);

            return element;
        }

        #region IImageElement Members

        /// <inheritdoc />
        public Length Height { get; set; }

        /// <inheritdoc />
        public Uri Href { get; set; }

        /// <inheritdoc />
        public Length Width { get; set; }

        /// <inheritdoc />
        public Length X { get; set; }

        /// <inheritdoc />
        public Length Y { get; set; }

        #endregion
    }

    public interface IImageElement
    {
        Length Height { get; set; }
        Uri Href { get; set; }
        Length Width { get; set; }
        Length X { get; set; }
        Length Y { get; set; }
    }
}