using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Ibinimator.Model;
using SharpDX;

namespace Ibinimator.Service
{
    public class FileService
    {
        static FileService()
        {
            var xao = new XmlAttributeOverrides();

            xao.AddAttribute<Matrix3x2>(nameof(Matrix3x2.M11));
            xao.AddAttribute<Matrix3x2>(nameof(Matrix3x2.M21));
            xao.AddAttribute<Matrix3x2>(nameof(Matrix3x2.M31));
            xao.AddAttribute<Matrix3x2>(nameof(Matrix3x2.M12));
            xao.AddAttribute<Matrix3x2>(nameof(Matrix3x2.M22));
            xao.AddAttribute<Matrix3x2>(nameof(Matrix3x2.M32));

            xao.AddIgnore<Matrix3x2>(nameof(Matrix3x2.Row1));
            xao.AddIgnore<Matrix3x2>(nameof(Matrix3x2.Row2));
            xao.AddIgnore<Matrix3x2>(nameof(Matrix3x2.Row3));
            xao.AddIgnore<Matrix3x2>(nameof(Matrix3x2.Column1));
            xao.AddIgnore<Matrix3x2>(nameof(Matrix3x2.Column2));
            xao.AddIgnore<Matrix3x2>(nameof(Matrix3x2.TranslationVector));
            xao.AddIgnore<Matrix3x2>(nameof(Matrix3x2.ScaleVector));

            xao.AddAttribute<Color4>(nameof(Color4.Red));
            xao.AddAttribute<Color4>(nameof(Color4.Green));
            xao.AddAttribute<Color4>(nameof(Color4.Blue));
            xao.AddAttribute<Color4>(nameof(Color4.Alpha));

            Serializer = 
                new XmlSerializer(
                    typeof(Document), 
                    new[]
                    {
                        typeof(Layer),
                        typeof(BrushInfo)
                    });
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
        public static void AddAttribute<T>(this XmlAttributeOverrides xao, string name)
        {
            xao.Add(typeof(T), new XmlAttributes { XmlAttribute = new XmlAttributeAttribute(name)});
        }

        public static void AddIgnore<T>(this XmlAttributeOverrides xao, string name)
        {
            xao.Add(typeof(T), name, new XmlAttributes { XmlIgnore = true });
        }
    }
}