using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

using Rain.Core.Model.Geometry;
using Rain.Core.Utility;

namespace Rain.Formatter.Svg.Shapes
{
    public class Path : ShapeElementBase
    {
        public PathInstruction[] Data { get; set; }

        public override void FromXml(XElement element, SvgContext context)
        {
            base.FromXml(element, context);

            Data = PathDataParser.Parse((string) element.Attribute("d") ?? "").ToArray();
        }

        public override XElement ToXml(SvgContext context)
        {
            var element = base.ToXml(context);

            element.Name = SvgNames.Path;

            if (Data.Length <= 0) return element;

            var pathData = "";

            foreach (var pathNode in Data)
                switch (pathNode)
                {
                    case ClosePathInstruction _:
                        pathData += " Z";

                        break;
                    case ArcPathInstruction an:
                        pathData += $"A{an.RadiusX} {an.RadiusY} {an.Angle} " +
                                    $"{(an.LargeArc ? 1 : 0)} {(an.Clockwise ? 1 : 0)} " +
                                    $"{an.X} {an.Y}";

                        break;
                    case QuadraticPathInstruction qn:
                        pathData += $"Q{qn.Control.X},{qn.Control.Y} " + $"{qn.X},{qn.Y} ";

                        break;
                    case CubicPathInstruction cn:
                        pathData += $"C{cn.Control1.X},{cn.Control1.Y} " +
                                    $"{cn.Control2.X},{cn.Control2.Y} " + $"{cn.X},{cn.Y} ";

                        break;
                    case LinePathInstruction ln:
                        pathData += $"L{ln.X},{ln.Y} ";

                        break;
                    case MovePathInstruction mn:
                        pathData += $"M{mn.X},{mn.Y} ";

                        break;
                }

            element.SetAttributeValue("d", pathData);

            return element;
        }
    }
}