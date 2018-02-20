using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Ibinimator.Core.Model;

namespace Ibinimator.Formatter.Svg.Paint
{
    public class SolidColorPaint : Paint
    {
        public SolidColorPaint() { }

        public SolidColorPaint(string id, Color color)
        {
            Id = id;
            Color = color;
        }

        public Color Color { get; set; }

        /// <inheritdoc />
        public override float Opacity
        {
            get => Color.Alpha;
            set
            {
                var color = Color;
                color.Alpha = value;
                Color = color;
            }
        }

        public override void FromXml(XElement element, SvgContext context)
        {
            if (element.Name.ToString().ToLowerInvariant() != "solidcolor")
                throw new InvalidDataException("This is not a valid solid colour element.");

            var color = LazyGet<Color>(element, "solid-color");
            color.Alpha = LazyGet<float>(element, "solid-opacity", 1);
            Color = color;
        }

        /// <inheritdoc />
        public override string ToInline()
        {
            return $"rgb({Color.Red * 100}%," + $"{Color.Green * 100}%," + $"{Color.Blue * 100}%)";
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"rgba({Color.Red * 100}%," + $"{Color.Green * 100}%," +
                   $"{Color.Blue * 100}%," + $"{Color.Alpha})";
        }

        public override XElement ToXml(SvgContext svgContext)
        {
            var element = base.ToXml(svgContext);

            LazySet(element, "solid-color", new Color(Color.Red, Color.Green, Color.Blue));
            LazySet(element, "solid-opacity", Color.Alpha, 1f);

            return element;
        }
    }
}