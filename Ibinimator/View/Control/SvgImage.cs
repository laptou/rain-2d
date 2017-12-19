using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;

using Ibinimator.Core;
using Ibinimator.Renderer.WPF;
using Ibinimator.Service;
using Ibinimator.Svg;

using Document = Ibinimator.Core.Document;
using WPF = System.Windows;

// ReSharper disable PossibleInvalidOperationException

namespace Ibinimator.View.Control
{
    /// <inheritdoc />
    /// <summary>
    ///     Interaction logic for SvgImage.xaml
    /// </summary>
    public class SvgImage : WPF.FrameworkElement, IArtContext
    {
        public static readonly WPF.DependencyProperty SourceProperty =
            WPF.DependencyProperty.Register(
                "Source",
                typeof(Uri),
                typeof(SvgImage),
                new WPF.FrameworkPropertyMetadata(
                    null,
                    WPF.FrameworkPropertyMetadataOptions.AffectsMeasure |
                    WPF.FrameworkPropertyMetadataOptions.AffectsRender,
                    SourceChanged));

        private readonly CacheManager _cache;
        private          Document     _document;

        private bool _prepared;

        public SvgImage()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            _cache = new CacheManager(this);
            ViewManager = new ViewManager(this);
        }

        [Category("Common")]
        public Uri Source
        {
            get => (Uri) GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        protected override WPF.Size ArrangeOverride(WPF.Size finalSize) { return MeasureOverride(finalSize); }

        protected override WPF.Size MeasureOverride(WPF.Size availableSize)
        {
            if (_document == null)
                return new WPF.Size();

            var width = availableSize.Width;
            var height = availableSize.Height;
            var aspect = width / height;

            var docWidth = _document.Bounds.Width;
            var docHeight = _document.Bounds.Height;
            var docAspect = docWidth / docHeight;

            return aspect > docAspect ?
                       new WPF.Size(docWidth * height / docHeight, height) :
                       new WPF.Size(width, docHeight * width / docWidth);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (!_prepared) return;

            var scale = (float) Math.Min(ActualWidth / _document.Bounds.Width,
                                         ActualHeight / _document.Bounds.Height);

            RenderContext.Begin(drawingContext);

            RenderContext.Transform(
                Matrix3x2.CreateTranslation(-_document.Bounds.TopLeft));

            // don't scale towards center - they are not center-aligned in the first place!
            RenderContext.Transform(Matrix3x2.CreateScale(scale));

            _document.Root.Render(RenderContext, CacheManager, ViewManager);

            RenderContext.End();
        }

        private async Task<Stream> GetStream()
        {
            if (Source.IsAbsoluteUri)
                switch (Source.Scheme)
                {
                    case "file":

                        return File.OpenRead(Source.LocalPath);
                    case "http":
                        var request = WebRequest.CreateHttp(Source);
                        var response = await Task.Factory.FromAsync(
                                           request.BeginGetResponse,
                                           request.EndGetResponse,
                                           null);

                        return response.GetResponseStream();
                    default:

                        throw new Exception("Unsupported URI scheme!");
                }

            return WPF.Application.GetResourceStream(Source)?.Stream;
        }

        private static async void SourceChanged(
            WPF.DependencyObject                   d,
            WPF.DependencyPropertyChangedEventArgs e)
        {
            if (d is SvgImage svgImage)
                await svgImage.UpdateAsync();
        }

        private async Task UpdateAsync()
        {
            if (Source == null) return;

            _prepared = false;

            XDocument xdoc;

            using (var stream = await GetStream())
            {
                xdoc = XDocument.Load(stream);
            }

            var document = new Svg.Document();
            document.FromXml(xdoc.Root, new SvgContext());

            _document = SvgConverter.FromSvg(document);
            _prepared = true;
        }

        #region IArtContext Members

        public event EventHandler StatusChanged;
        public       void         InvalidateSurface() { InvalidateVisual(); }

        public IBrushManager   BrushManager   { get; }
        public ICacheManager   CacheManager   => _cache;
        public IHistoryManager HistoryManager { get; }

        public RenderContext RenderContext { get; } = new WpfRenderContext();

        public ISelectionManager SelectionManager { get; }
        public Status            Status           { get; set; }
        public IToolManager      ToolManager      { get; }
        public IViewManager      ViewManager      { get; }

        #endregion
    }
}