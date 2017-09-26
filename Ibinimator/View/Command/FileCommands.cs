using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Ibinimator.Service;
using Ibinimator.Svg;
using Ibinimator.Utility;
using Ibinimator.ViewModel;

namespace Ibinimator.View.Command
{
    public static class FileCommands
    {
        public static readonly AsyncDelegateCommand<IViewManager> SaveCommand =
            CommandManager.RegisterAsync<IViewManager>(SaveAsync);

        public static readonly AsyncDelegateCommand<IViewManager> OpenCommand =
            CommandManager.RegisterAsync<IViewManager>(OpenAsync);

        private static async Task OpenAsync(IViewManager vm)
        {
            var ofd = new OpenFileDialog
            {
                DefaultExt = ".svg",
                Filter = "SVG file|*.svg",
                CheckFileExists = true
            };

            await App.Dispatcher.InvokeAsync(() => ofd.ShowDialog());

            if (!string.IsNullOrWhiteSpace(ofd.FileName))
            {
                using (var stream = ofd.OpenFile())
                {
                    var xdoc = XDocument.Load(stream);
                    var doc = new Document();
                    doc.FromXml(xdoc.Root, new SvgContext());
                    vm.Document = SvgConverter.FromSvg(doc);

                    vm.ArtView.CacheManager.ResetAll();
                    vm.ArtView.CacheManager.LoadBitmaps(vm.ArtView.RenderTarget);
                    vm.ArtView.CacheManager.LoadBrushes(vm.ArtView.RenderTarget);
                    vm.ArtView.CacheManager.Bind(vm.Document);
                }
            }
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

            using (var stream = File.Open(doc.Path, FileMode.Create))
            {
                var svgDoc = SvgConverter.ToSvg(doc);
                var xdoc = new XDocument(svgDoc.ToXml(new SvgContext()));
                xdoc.Save(stream);
            }
        }
    }
}