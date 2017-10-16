using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.Renderer.WPF;
using Ibinimator.Service;
using Ibinimator.Svg;
using Ibinimator.Utility;
using WPF = System.Windows;
using static Ibinimator.Core.Model.LengthUnit;
using Document = Ibinimator.Renderer.Model.Document;

// ReSharper disable PossibleInvalidOperationException

namespace Ibinimator.View.Control
{
    /// <inheritdoc />
    /// <summary>
    ///     Interaction logic for SvgImage.xaml
    /// </summary>
    public class SvgImage : WPF.FrameworkElement, IArtContext
    {
        private Document _document;
        private CacheManager _cache;

        public SvgImage()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            _cache = new CacheManager(this);
            
        }

        public Uri Source
        {
            get => (Uri) GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        
        public static readonly WPF.DependencyProperty SourceProperty =
            WPF.DependencyProperty.Register("Source", typeof(Uri), typeof(SvgImage),
                new WPF.FrameworkPropertyMetadata(null,
                    WPF.FrameworkPropertyMetadataOptions.AffectsMeasure |
                    WPF.FrameworkPropertyMetadataOptions.AffectsRender, SourceChanged));

        private static void SourceChanged(WPF.DependencyObject d, WPF.DependencyPropertyChangedEventArgs e)
        {
            if (d is SvgImage svgImage)
                svgImage.Update();
        }

        private bool _prepared;

        protected override WPF.Size MeasureOverride(WPF.Size availableSize)
        {
            if (_document == null)
                return WPF.Size.Empty;

            var width = availableSize.Width;
            var height = availableSize.Height;
            var aspect = width / height;

            var docWidth = _document.Bounds.Width;
            var docHeight = _document.Bounds.Height;
            var docAspect = docWidth / docHeight;

            return aspect > docAspect
                ? new WPF.Size(docWidth * height / docHeight, height)
                : new WPF.Size(width, docHeight * width / docWidth);
        }

        protected override WPF.Size ArrangeOverride(WPF.Size finalSize)
        {
            return MeasureOverride(finalSize);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (Source.ToString().Contains("restore"))
                Debugger.Break();

            RenderContext.Begin(drawingContext);
            _document.Root.Render(RenderContext, CacheManager);
            RenderContext.End();
        }

        private void Update()
        {
            if (Source == null) return;

            _prepared = false;

            XDocument xdoc;

            using (var stream = GetStream())
                xdoc = XDocument.Load(stream);

            if (Source.ToString().Contains("restore"))
                Debugger.Break();

            var document = new Svg.Document();
            document.FromXml(xdoc.Root, new SvgContext());

            _document = SvgConverter.FromSvg(document);
            _prepared = true;
        }

        private Stream GetStream()
        {
            try
            {
                return WPF.Application.GetResourceStream(Source)?.Stream;
            }
            catch
            {
                return File.OpenRead(Source.AbsolutePath);
            }
        }

        public RenderContext RenderContext { get; } = new WpfRenderContext();
        public ICacheManager CacheManager => _cache;

        public ISelectionManager SelectionManager { get; }
        public IHistoryManager HistoryManager { get; }
        public IViewManager ViewManager { get; }
        public IBrushManager BrushManager { get; }
        public IToolManager ToolManager { get; }

        public void InvalidateSurface()
        {
            InvalidateVisual();
        }
    }
}