using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Ibinimator.Svg
{
    public class Path : ShapeElementBase
    {
        public PathNode[] Data { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            Data = PathDataParser.Parse((string) element.Attribute("d") ?? "").ToArray();
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);
            element.Name = "path";

            if (Data.Length <= 0) return element;

            var begin = true;
            var pathData = "";

            foreach (var pathNode in Data)
            {
                if (begin)
                {
                    pathData += $" M {pathNode.X},{pathNode.Y}";
                    begin = false;
                    continue;
                }

                switch (pathNode)
                {
                    case CloseNode c:
                        pathData += $" Z";
                        begin = true;
                        break;
                    case QuadraticPathNode qn:
                        pathData += $" Q {qn.Control.X},{qn.Control.Y}" +
                                    $" {qn.X},{qn.Y}";
                        break;
                    case CubicPathNode cn:
                        pathData += $" C {cn.Control1.X},{cn.Control1.Y}" +
                                    $" {cn.Control2.X},{cn.Control2.Y}" +
                                    $" {cn.X},{cn.Y}";
                        break;
                    default:
                        pathData += $" L {pathNode.X},{pathNode.Y}";
                        break;
                }
            }

            element.SetAttributeValue("d", pathData);

            return element;
        }
    }
}