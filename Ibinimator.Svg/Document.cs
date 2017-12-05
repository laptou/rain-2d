using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Ibinimator.Core.Model;

namespace Ibinimator.Svg
{
    public static class SvgNames
    {
        public static readonly XNamespace Namespace = "http://www.w3.org/2000/svg";
        public static readonly XNamespace XLink = "http://www.w3.org/1999/xlink";

        #region visuals
        public static readonly XName Svg = Namespace + "svg";
        public static readonly XName Defs = Namespace + "defs";
        public static readonly XName Rect = Namespace + "rect";
        public static readonly XName Ellipse = Namespace + "ellipse";
        public static readonly XName Circle = Namespace + "circle";
        public static readonly XName Path = Namespace + "path";
        public static readonly XName Polygon = Namespace + "polygon";
        public static readonly XName Polyline = Namespace + "polyline";
        public static readonly XName Group = Namespace + "g";
        public static readonly XName Line = Namespace + "line";
        public static readonly XName Text = Namespace + "text";
        public static readonly XName Tspan = Namespace + "tspan";
        #endregion

        public static readonly XName SolidColor = Namespace + "solidColor";
        public static readonly XName LinearGradient = Namespace + "linearGradient";
        public static readonly XName RadialGradient = Namespace + "radialGradient";
        public static readonly XName Stop = Namespace + "stop";
    }

    public class Defs : ContainerElement
    {
        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Defs;

            return element;
        }
    }

    public class Document : GraphicalContainerElement
    {
        public Length Height { get; set; } = (100, LengthUnit.Percent);

        public float Version { get; set; } = 1.1f;

        public RectangleF Viewbox { get; set; }

        public Length Width { get; set; } = (100, LengthUnit.Percent);

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Svg;

            LazySet(element, "viewBox", Viewbox);
            LazySet(element, "version", Version);
            LazySet(element, "width", Width);
            LazySet(element, "height", Height);

            return element;
        }

        public override void FromXml(XElement element, SvgContext context)
        {
            context.Root = element;

            base.FromXml(element, context);

            if ((string) element.Attribute("viewBox") != null)
            {
                var vb = ((string) element.Attribute("viewBox"))
                    .Split(' ')
                    .Select(float.Parse)
                    .ToArray();

                Viewbox = new RectangleF(vb[0], vb[1], vb[2], vb[3]);
            }

            if (float.TryParse(LazyGet(element, "version"), out var version))
                Version = version;
            
            if (Length.TryParse(LazyGet(element, "width"), out var width))
                Width = width;

            if (Length.TryParse(LazyGet(element, "height"), out var height))
                Height = height;
        }
    }
}