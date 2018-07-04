using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

using Rain.Core;
using Rain.Formatter.Svg;
using Rain.Formatter.Svg.IO;
using Rain.Formatter.Svg.Structure;
using Rain.Renderer;
using Rain.ViewModel;

namespace Rain.View.Command
{
    public static class FileCommands
    {
        public static readonly AsyncDelegateCommand<IArtContext> SaveCommand =
            CommandManager.RegisterAsync<IArtContext>(SaveAsync);

        public static readonly AsyncDelegateCommand<IArtContext> OpenCommand =
            CommandManager.RegisterAsync<IArtContext>(OpenAsync);

        private static async Task OpenAsync(IArtContext artCtx)
        {
            var ofd = new OpenFileDialog
            {
                DefaultExt = ".svg",
                Filter = "SVG file|*.svg",
                CheckFileExists = true
            };

            await App.CurrentDispatcher.InvokeAsync(() => ofd.ShowDialog());

            if (!string.IsNullOrWhiteSpace(ofd.FileName))
                using (var stream = ofd.OpenFile())
                {
                    var xdoc = XDocument.Load(stream);
                    var doc = new Document();
                    doc.FromXml(xdoc.Root, new SvgContext(xdoc.Root));

                    var vm = new ViewManager(artCtx);

                    vm.Document = SvgReader.FromSvg(doc);

                    var artDim = Math.Max(vm.Document.Bounds.Width, vm.Document.Bounds.Height);
                    var viewDim = Math.Min(artCtx.RenderContext.Height, artCtx.RenderContext.Width);

                    vm.Zoom = viewDim / (artDim + 20);

                    artCtx.SetManager<IViewManager>(vm);
                }
        }

        private static async Task SaveAsync(IArtContext artCtx)
        {
            var doc = artCtx.ViewManager.Document;

            if (doc.Path == null)
            {
                var sfd = new SaveFileDialog
                {
                    DefaultExt = ".svg",
                    Filter = "SVG file|*.svg"
                };

                await App.CurrentDispatcher.InvokeAsync(() => sfd.ShowDialog());

                if (string.IsNullOrWhiteSpace(sfd.FileName))
                    return;

                doc.Path = sfd.FileName;
            }

            using (var stream = File.Open(doc.Path, FileMode.Create))
            {
                var svgDoc = SvgWriter.ToSvg(doc);
                var xdoc = new XDocument(svgDoc.ToXml(new SvgContext()));
                xdoc.Save(stream);
            }
        }
    }
}