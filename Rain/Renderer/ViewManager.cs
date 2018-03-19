using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core;
using Rain.Core.Model;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Utility;

namespace Rain.Renderer
{
    public class ViewManager : Core.Model.Model, IViewManager
    {
        public ViewManager(IArtContext artContext)
        {
            Context = artContext;
            Document = new Document();
            Zoom = 1;
        }

        public event PropertyChangingEventHandler DocumentUpdating;

        private void OnDocumentUpdated(object sender, PropertyChangedEventArgs e)
        {
            DocumentUpdated?.Invoke(sender, e);

            if (sender == Document &&
                e.PropertyName == nameof(Document.Root))
            {
                RootUpdated?.Invoke(sender, e);
                RaisePropertyChanged(nameof(Root));
            }

            if (sender is IFilledLayer filled &&
                e.PropertyName == nameof(IFilledLayer.Fill))
                if (filled.Fill?.Scope == ResourceScope.Document)
                    Document.Swatches.Add(filled.Fill);

            if (sender is IStrokedLayer stroked &&
                e.PropertyName == nameof(IStrokedLayer.Stroke))
                if (stroked.Stroke?.Brush?.Scope == ResourceScope.Document)
                    Document.Swatches.Add(stroked.Stroke.Brush);
        }

        private void OnDocumentUpdating(object sender, PropertyChangingEventArgs e)
        {
            DocumentUpdating?.Invoke(sender, e);

            if (sender is IFilledLayer filled &&
                e.PropertyName == nameof(IFilledLayer.Fill)) Document.Swatches.Remove(filled.Fill);

            if (sender is IStrokedLayer stroked &&
                e.PropertyName == nameof(IStrokedLayer.Stroke))
                Document.Swatches.Remove(stroked.Stroke?.Brush);
        }

        #region IViewManager Members

        public event PropertyChangedEventHandler DocumentUpdated;
        public event PropertyChangedEventHandler RootUpdated;

        /// <inheritdoc />
        public void Attach(IArtContext context)
        {
            // ViewManager doesn't subscribe to events from any other managers.

            if (context != Context)
                throw new InvalidOperationException(
                    "A new ViewManager must be created for each ArtContext.");

            Context.RaiseAttached(this);
        }

        /// <inheritdoc />
        public void Detach(IArtContext context)
        {
            // ViewManager doesn't subscribe to events from any other managers.

            if (context != Context)
                throw new InvalidOperationException(
                    "This ViewManager is not attached to that ArtContext.");

            Context.RaiseDetached(this);
        }

        public RectangleF FromArtSpace(RectangleF v) { return MathUtils.Bounds(v, Transform); }

        public Vector2 FromArtSpace(Vector2 v) { return Vector2.Transform(v, Transform); }

        public void Render(RenderContext target, ICacheManager cache)
        {
            using (var brush = target.CreateBrush(new Color(0.9f)))
            using (var pen = target.CreatePen(1, brush))
            {
                target.DrawRectangle(Document.Bounds, pen);
            }

            using (var brush = target.CreateBrush(new Color(1f)))
            {
                target.FillRectangle(Document.Bounds, brush);
            }
        }

        public RectangleF ToArtSpace(RectangleF v)
        {
            return MathUtils.Bounds(v, MathUtils.Invert(Transform));
        }

        public Vector2 ToArtSpace(Vector2 v)
        {
            return Vector2.Transform(v, MathUtils.Invert(Transform));
        }

        public IArtContext Context { get; }

        public Document Document
        {
            get => Get<Document>();
            set
            {
                if (Document != null)
                {
                    Document.Updating -= OnDocumentUpdating;
                    Document.Updated -= OnDocumentUpdated;
                }

                Set(value);
                RaisePropertyChanged(nameof(Root));

                if (Document != null)
                {
                    Document.Updating += OnDocumentUpdating;
                    Document.Updated += OnDocumentUpdated;
                }
            }
        }

        public Vector2 Pan
        {
            get => Get<Vector2>();
            set
            {
                Set(Vector2.Clamp(value,
                                  -Document.Bounds.Size * Zoom,
                                  Document.Bounds.Size * Zoom));
                RaisePropertyChanged(nameof(Transform));
            }
        }

        public IContainerLayer Root
        {
            get => Document.Root;
            set => Document.Root = value;
        }

        public Matrix3x2 Transform =>
            Matrix3x2.CreateScale(Zoom) * Matrix3x2.CreateTranslation(Pan);

        public float Zoom
        {
            get => Get<float>();
            set
            {
                Set(MathUtils.Clamp(1e-4f, 1e4f, value));
                RaisePropertyChanged(nameof(Transform));
            }
        }

        #endregion
    }
}