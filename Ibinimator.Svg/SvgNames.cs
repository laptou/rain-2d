using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    internal static class SvgNames
    {
        public static readonly XNamespace Namespace  = "http://www.w3.org/2000/svg";
        public static readonly XNamespace XLink      = "http://www.w3.org/1999/xlink";
        public static readonly XNamespace Ibinimator = "http://ibiyemi.intulon.com/schema/rain2d";

        #region metadata

        public static readonly XName Name = Ibinimator + "name";

        #endregion

        #region paint

        public static readonly XName SolidColor     = Namespace + "solidColor";
        public static readonly XName LinearGradient = Namespace + "linearGradient";
        public static readonly XName RadialGradient = Namespace + "radialGradient";
        public static readonly XName Stop           = Namespace + "stop";

        #endregion

        #region visuals

        public static readonly XName Svg      = Namespace + "svg";
        public static readonly XName Defs     = Namespace + "defs";
        public static readonly XName Rect     = Namespace + "rect";
        public static readonly XName Ellipse  = Namespace + "ellipse";
        public static readonly XName Circle   = Namespace + "circle";
        public static readonly XName Path     = Namespace + "path";
        public static readonly XName Polygon  = Namespace + "polygon";
        public static readonly XName Polyline = Namespace + "polyline";
        public static readonly XName Group    = Namespace + "g";
        public static readonly XName Line     = Namespace + "line";
        public static readonly XName Text     = Namespace + "text";
        public static readonly XName Tspan    = Namespace + "tspan";

        #endregion
    }
}