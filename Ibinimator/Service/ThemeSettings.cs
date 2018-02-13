using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Ibinimator.Service
{
    public class ThemeSettings : Settings
    {
        private readonly string _name;

        internal ThemeSettings(string name)
        {
            _name = name;
            Load();
        }

        public override object this[string path] => GetColor("colors." + path);

        /// <inheritdoc />
        protected override Stream GetReadStream()
        {
            var defaultUri = new Uri($"/Ibinimator;component/theme/{_name}.json",
                                     UriKind.Relative);

            if (App.IsDesigner)
                return Application.GetResourceStream(defaultUri)?.Stream;

            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                                        $"theme/{_name}.json");

            if (!File.Exists(filePath))
                using (var file = File.Open(filePath, FileMode.Create, FileAccess.Write))
                {
                    var defaultFile = Application.GetResourceStream(defaultUri)?.Stream;

                    if (defaultFile == null)
                        throw new Exception("Default settings file is missing.");

                    defaultFile.CopyTo(file);
                    file.Flush(true);
                    defaultFile.Dispose();
                }

            return File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        protected override Stream GetWriteStream()
        {
            return File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                          $"theme/{_name}.json"),
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.Read);
        }
    }
}
