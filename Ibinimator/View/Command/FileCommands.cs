using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

using Ibinimator.Core;
using Ibinimator.Renderer;
using Ibinimator.Service;
using Ibinimator.Svg;
using Ibinimator.Svg.IO;
using Ibinimator.Svg.Structure;
using Ibinimator.ViewModel;

namespace Ibinimator.View.Command
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

            await App.Dispatcher.InvokeAsync(() => ofd.ShowDialog());

            if (!string.IsNullOrWhiteSpace(ofd.FileName))
                using (var stream = ofd.OpenFile())
                {
                    var xdoc = XDocument.Load(stream);
                    var doc = new Document();
                    doc.FromXml(xdoc.Root, new SvgContext {Root = xdoc.Root});

                    var vm = new ViewManager(artCtx);

                    vm.Document = SvgReader.FromSvg(doc);

                    artCtx.CacheManager.ReleaseResources();
                    artCtx.CacheManager.LoadBitmaps(artCtx.RenderContext);
                    artCtx.CacheManager.LoadBrushes(artCtx.RenderContext);
                    artCtx.CacheManager.BindLayer(vm.Document.Root);
                    vm.Pan = Vector2.One * 10;

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

                await App.Dispatcher.InvokeAsync(() => sfd.ShowDialog());

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