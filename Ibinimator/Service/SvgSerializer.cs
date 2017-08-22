using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Ibinimator.Model;
using SharpDX;

namespace Ibinimator.Service
{
    public interface ISvgSerializable
    {
        XElement GetElement();
    }

    public static class SvgSerializer
    {
        public static XDocument SerializeDocument(Document doc)
        {
            var root = new XElement("svg");
            var defs = new XElement("defs");

            foreach (var brush in doc.Swatches)
                defs.Add(brush.GetElement());

            root.Add(defs);
            root.Add(doc.Root.GetElement());

            return new XDocument(root);
        }

        public static string ToCss(this Color4 color)
        {
            return "rgb(" +
                   $"{color.Red * 100}%," +
                   $"{color.Green * 100}%," +
                   $"{color.Blue * 100}%" +
                   ")";
        }

        public static string ToCss(this Matrix3x2 matrix)
        {
            return
                "matrix(" +
                $"{matrix.M11},{matrix.M12}," +
                $"{matrix.M21},{matrix.M22}," +
                $"{matrix.M31},{matrix.M32}" +
                ")";
        }
    }
}