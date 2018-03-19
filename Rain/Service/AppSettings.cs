using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Rain.Service
{
    public class AppSettings : Settings
    {
        public static AppSettings Current { get; private set; }

        public ThemeSettings Theme { get; private set; }

        public static void LoadDefault()
        {
            Current = new AppSettings();
            Current.Load();

            Current.Theme = new ThemeSettings(Current["theme"] as string);
        }

        public static async Task LoadDefaultAsync()
        {
            Current = new AppSettings();
            await Current.LoadAsync();

            Current.Theme = new ThemeSettings(Current["theme"] as string);
        }

        protected override Stream GetReadStream()
        {
            var defaultUri = new Uri("/Rain;component/settings.default.json",
                                     UriKind.Relative);

            if (App.IsDesigner)
                return Application.GetResourceStream(defaultUri)?.Stream;

            var filePath = AppDomain.CurrentDomain.BaseDirectory + "settings.json";

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

        /// <inheritdoc />
        protected override Stream GetWriteStream()
        {
            return File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json"),
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.Read);
        }

        private static Task _loadTask;

        public static void BeginLoadDefault() => _loadTask = LoadDefaultAsync();

        public static void EndLoadDefault() => _loadTask.Wait();

        public static Task GetLoadTask() => _loadTask;
    }
}