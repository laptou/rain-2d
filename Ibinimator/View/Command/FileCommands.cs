using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Ibinimator.Renderer;
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
                using (var stream = ofd.OpenFile())
                {
                    var xdoc = XDocument.Load(stream);
                    var doc = new Document();
                    doc.FromXml(xdoc.Root, new SvgContext {Root = xdoc.Root});
                    vm.Document = SvgConverter.FromSvg(doc);

                    vm.Context.CacheManager.ResetAll();
                    vm.Context.CacheManager.LoadBitmaps(vm.Context.RenderContext);
                    vm.Context.CacheManager.LoadBrushes(vm.Context.RenderContext);
                    vm.Context.CacheManager.Bind(vm.Document);
                    vm.Pan = Vector2.One * 10;

                    var artDim = Math.Max(vm.Document.Bounds.Width, vm.Document.Bounds.Height);
                    // var viewDim = Math.Min(vm.Context.ActualWidth, vm.Context.ActualHeight);

                    vm.Zoom = 1; //(float)(viewDim / (artDim + 20));
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