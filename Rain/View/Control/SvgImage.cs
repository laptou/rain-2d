﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;

using Rain.Core;
using Rain.Core.Input;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Text;
using Rain.Formatter.Svg;
using Rain.Formatter.Svg.IO;
using Rain.Renderer;
using Rain.Renderer.WPF;

using WPF = System.Windows;

// ReSharper disable PossibleInvalidOperationException

namespace Rain.View.Control
{
    /// <inheritdoc />
    /// <summary>
    ///     Interaction logic for SvgImage.xaml
    /// </summary>
    public class SvgImage : WPF.FrameworkElement, IArtContext
    {
        public static readonly WPF.DependencyProperty SourceProperty =
            WPF.DependencyProperty.Register("Source",
                                            typeof(Uri),
                                            typeof(SvgImage),
                                            new WPF.FrameworkPropertyMetadata(
                                                null,
                                                WPF.FrameworkPropertyMetadataOptions.AffectsRender,
                                                SourceChanged));

        private readonly CacheManager _cache;
        private readonly IViewManager _view;
        private          Document     _document;

        private bool _prepared;

        public SvgImage()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            _cache = new CacheManager(this);
            _view = new ViewManager(this);
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

            var docWidth = _document.Bounds.Width;
            var docHeight = _document.Bounds.Height;

            var width = availableSize.Width;
            var height = availableSize.Height;

            if (double.IsPositiveInfinity(height))
                height = docHeight;
            if (double.IsPositiveInfinity(width))
                width = docWidth;

            var aspect = width / height;
            var docAspect = docWidth / docHeight;

            return aspect > docAspect
                       ? new WPF.Size(docWidth * height / docHeight, height)
                       : new WPF.Size(width, docHeight * width / docWidth);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (!_prepared) return;

            var scale = (float) Math.Min(ActualWidth / _document.Bounds.Width, ActualHeight / _document.Bounds.Height);

            RenderContext.Begin(drawingContext);

            RenderContext.Transform(Matrix3x2.CreateTranslation(-_document.Bounds.TopLeft));

            // don't scale towards center - they are not center-aligned in the first place!
            RenderContext.Transform(Matrix3x2.CreateScale(scale));

            _document.Root.Render(RenderContext, _cache, _view);

            RenderContext.End();
        }

        private Stream GetStream()
        {
            if (!Source.IsAbsoluteUri)
                return WPF.Application.GetResourceStream(Source)?.Stream;

            switch (Source.Scheme)
            {
                case "file":

                    return File.OpenRead(Source.LocalPath);
                default:

                    throw new Exception("Unsupported URI scheme!");
            }
        }

        private static async void SourceChanged(WPF.DependencyObject d, WPF.DependencyPropertyChangedEventArgs e)
        {
            if (d is SvgImage svgImage)
                await svgImage.UpdateAsync();
        }

        private async Task UpdateAsync()
        {
            if (Source == null) return;

            _prepared = false;

            var stream = GetStream();

            await Task.Run(() =>
                     {
                         _cache.ReleaseSceneResources();
                          
                         _document = SvgReader.FromSvg(stream);

                         stream.Dispose();

                         _cache.BindLayer(_document.Root);
                     });

            _prepared = true;
        }

        #region IArtContext Members

        /// <inheritdoc />
        public ICaret CreateCaret(int width, int height) { throw new NotImplementedException(); }

        public void Invalidate()
        {
            if (!_prepared) return;

            if (CheckAccess())
                InvalidateVisual();
            else
                Dispatcher.Invoke(InvalidateVisual);
        }

        /// <inheritdoc />
        public void RaiseAttached(IArtContextManager mgr) { throw new NotImplementedException(); }

        /// <inheritdoc />
        public void RaiseDetached(IArtContextManager mgr) { throw new NotImplementedException(); }


        /// <inheritdoc />
        public void SetManager<T>(T manager) where T : IArtContextManager { throw new InvalidOperationException(); }

        public IRenderContext RenderContext { get; } = new WpfRenderContext();

        /// <inheritdoc />
        public ResourceContext ResourceContext { get; }

        /// <inheritdoc />
        IBrushManager IArtContext.BrushManager { get; }

        /// <inheritdoc />
        ICacheManager IArtContext.CacheManager => _cache;

        /// <inheritdoc />
        IHistoryManager IArtContext.HistoryManager { get; }

        /// <inheritdoc />
        ISelectionManager IArtContext.SelectionManager { get; }

        /// <inheritdoc />
        Status IArtContext.Status { get; set; }

        /// <inheritdoc />
        IToolManager IArtContext.ToolManager { get; }

        /// <inheritdoc />
        IViewManager IArtContext.ViewManager => _view;

        #endregion

#pragma warning disable CS0067

        /// <inheritdoc />
        public event ArtContextInputEventHandler<FocusEvent> GainedFocus;

        /// <inheritdoc />
        public new event ArtContextInputEventHandler<KeyboardEvent> KeyDown;

        /// <inheritdoc />
        public new event ArtContextInputEventHandler<KeyboardEvent> KeyUp;

        /// <inheritdoc />
        public new event ArtContextInputEventHandler<FocusEvent> LostFocus;

        /// <inheritdoc />
        public new event ArtContextInputEventHandler<ClickEvent> MouseDown;

        /// <inheritdoc />
        public new event ArtContextInputEventHandler<PointerEvent> MouseMove;

        /// <inheritdoc />
        public new event ArtContextInputEventHandler<ClickEvent> MouseUp;

        /// <inheritdoc />
        public event EventHandler ManagerDetached;

        /// <inheritdoc />
        public event EventHandler StatusChanged;

        /// <inheritdoc />
        public event ArtContextInputEventHandler<TextEvent> Text;

        /// <inheritdoc />
        public event EventHandler ManagerAttached;

#pragma warning restore CS0067
    }
}