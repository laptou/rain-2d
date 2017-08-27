using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Ibinimator.ViewModel;

namespace Ibinimator.Service
{
    public static class FileCommands
    {
        public static readonly AsyncDelegateCommand<IViewManager> SaveCommand =
            new AsyncDelegateCommand<IViewManager>(SaveAsync);

        public static readonly AsyncDelegateCommand<IViewManager> OpenCommand =
            new AsyncDelegateCommand<IViewManager>(OpenAsync);

        private static async Task OpenAsync(IViewManager vm)
        {
            var ofd = new OpenFileDialog
            {
                DefaultExt = ".svg",
                Filter = "SVG file|*.svg",
                CheckFileExists = true
            };

            await App.Dispatcher.InvokeAsync(() => ofd.ShowDialog());

            var doc = SvgSerializer.DeserializeDocument(XDocument.Load(ofd.OpenFile()));
            vm.Document = doc;
        }

        private static async Task SaveAsync(IViewManager vm)
        {
            var doc = vm.Document;
            if (doc.Path == null)
            {
                var sfd = new SaveFileDialog
                {
                    DefaultExt = ".svg",
                    Filter = "SVG file|*.svg"
                };

                await App.Dispatcher.InvokeAsync(() => sfd.ShowDialog());

                doc.Path = sfd.FileName;
            }

            await FileService.SaveAsync(doc);
        }
    }
}