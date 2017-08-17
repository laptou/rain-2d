using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Ibinimator.Model;
using Ibinimator.ViewModel;

namespace Ibinimator.Service
{
    public class FileService
    {
        public static void Serialize(Layer root)
        {
            var memoryStream = new MemoryStream();

            var layerTypes = 
                (
                from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                where !domainAssembly.IsDynamic 
                from assemblyType in domainAssembly.GetExportedTypes()
                where assemblyType.IsSubclassOf(typeof(Layer)) ||
                      assemblyType.IsSubclassOf(typeof(BrushInfo))
                select assemblyType).ToArray();

            var serializer = new XmlSerializer(typeof(Layer), layerTypes);
            serializer.Serialize(memoryStream, root);
            memoryStream.Position = 0;

            var s = new StreamReader(memoryStream).ReadToEnd();
        }
    }
}
