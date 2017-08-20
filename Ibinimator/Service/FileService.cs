using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Ibinimator.Model;
using SharpDX;
using SharpDX.Direct2D1;
using Layer = Ibinimator.Model.Layer;
using StrokeStyle = SharpDX.Direct2D1.StrokeStyleProperties1;

namespace Ibinimator.Service
{
    public class FileService
    {
        static FileService()
        {
            var xao = new XmlAttributeOverrides();

            xao.Attribute<Matrix3x2>(nameof(Matrix3x2.M11));
            xao.Attribute<Matrix3x2>(nameof(Matrix3x2.M21));
            xao.Attribute<Matrix3x2>(nameof(Matrix3x2.M31));
            xao.Attribute<Matrix3x2>(nameof(Matrix3x2.M12));
            xao.Attribute<Matrix3x2>(nameof(Matrix3x2.M22));
            xao.Attribute<Matrix3x2>(nameof(Matrix3x2.M32));
            xao.Default<Matrix3x2>(nameof(Matrix3x2.M11), 1);
            xao.Default<Matrix3x2>(nameof(Matrix3x2.M21), 0);
            xao.Default<Matrix3x2>(nameof(Matrix3x2.M31), 0);
            xao.Default<Matrix3x2>(nameof(Matrix3x2.M12), 0);
            xao.Default<Matrix3x2>(nameof(Matrix3x2.M22), 1);
            xao.Default<Matrix3x2>(nameof(Matrix3x2.M32), 0);

            xao.Ignore<Matrix3x2>(nameof(Matrix3x2.Row1));
            xao.Ignore<Matrix3x2>(nameof(Matrix3x2.Row2));
            xao.Ignore<Matrix3x2>(nameof(Matrix3x2.Row3));
            xao.Ignore<Matrix3x2>(nameof(Matrix3x2.Column1));
            xao.Ignore<Matrix3x2>(nameof(Matrix3x2.Column2));
            xao.Ignore<Matrix3x2>(nameof(Matrix3x2.TranslationVector));
            xao.Ignore<Matrix3x2>(nameof(Matrix3x2.ScaleVector));

            xao.Attribute<Color4>(nameof(Color4.Red));
            xao.Attribute<Color4>(nameof(Color4.Green));
            xao.Attribute<Color4>(nameof(Color4.Blue));
            xao.Attribute<Color4>(nameof(Color4.Alpha));

            xao.Default<BrushInfo>(nameof(BrushInfo.Transform), Matrix3x2.Identity);

            xao.Attribute<StrokeStyle>(nameof(StrokeStyle.StartCap));
            xao.Attribute<StrokeStyle>(nameof(StrokeStyle.EndCap));
            xao.Attribute<StrokeStyle>(nameof(StrokeStyle.DashStyle));
            xao.Attribute<StrokeStyle>(nameof(StrokeStyle.DashOffset));
            xao.Attribute<StrokeStyle>(nameof(StrokeStyle.DashCap));
            xao.Attribute<StrokeStyle>(nameof(StrokeStyle.LineJoin));
            xao.Attribute<StrokeStyle>(nameof(StrokeStyle.MiterLimit));
            xao.Attribute<StrokeStyle>(nameof(StrokeStyle.TransformType));

            xao.Default<StrokeStyle>(nameof(StrokeStyle.DashStyle), DashStyle.Solid);
            xao.Default<StrokeStyle>(nameof(StrokeStyle.DashOffset), 0);
            xao.Default<StrokeStyle>(nameof(StrokeStyle.DashCap), CapStyle.Flat);
            xao.Default<StrokeStyle>(nameof(StrokeStyle.LineJoin), LineJoin.Miter);            
            xao.Default<StrokeStyle>(nameof(StrokeStyle.MiterLimit), 0);
            xao.Default<StrokeStyle>(nameof(StrokeStyle.TransformType), StrokeTransformType.Fixed);

            Serializer = 
                new XmlSerializer(
                    typeof(Document), 
                    xao,
                    new[]
                    {
                        typeof(Layer),
                        typeof(BrushInfo)
                    }, null, null, null);
        }

        private static readonly XmlSerializer Serializer;

        public static async Task SaveAsync(Document doc)
        {
            await Task.Run(() =>
            {
                using (var fs = File.Open(doc.Path, FileMode.Create))
                {
                    Serializer.Serialize(fs, doc);
                }
            });
        }

        public static async Task<Document> LoadAsync(string path)
        {
            return await Task.Run(() =>
            {
                using (var fs = File.Open(path, FileMode.Create))
                    return Serializer.Deserialize(fs) as Document;
            });
        }
    }

    public static class XmlAttributeOverridesExtensions
    {
        public static void Attribute<T>(this XmlAttributeOverrides xao, string name)
        {
            if (xao[typeof(T), name] == null)
                xao.Add(typeof(T), name, new XmlAttributes());

            xao[typeof(T), name].XmlAttribute = new XmlAttributeAttribute();
        }

        public static void Ignore<T>(this XmlAttributeOverrides xao, string name)
        {
            if (xao[typeof(T), name] == null)
                xao.Add(typeof(T), name, new XmlAttributes());

            xao[typeof(T), name].XmlIgnore = true;
        }

        public static void Default<T>(this XmlAttributeOverrides xao, string name, object defaultValue)
        {
            if (xao[typeof(T), name] == null)
                xao.Add(typeof(T), name, new XmlAttributes());

            xao[typeof(T), name].XmlDefaultValue = defaultValue;
        }

        public static void Default<T>(this XmlAttributeOverrides xao) where T : struct
        {
            if (xao[typeof(T)] == null)
                xao.Add(typeof(T), new XmlAttributes());

            xao[typeof(T)].XmlDefaultValue = default(T);
        }
    }
}