using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;

namespace Ibinimator.Renderer
{
    public class ViewManager : Core.Model.Model, IViewManager
    {
        public ViewManager(IArtContext artContext)
        {
            Context = artContext;
            Document = new Document();
            Zoom = 1;
        }

        private void OnDocumentUpdated(object sender, PropertyChangedEventArgs e)
        {
            DocumentUpdated?.Invoke(sender, e);
        }

        #region IViewManager Members

        public event PropertyChangedEventHandler DocumentUpdated;

        /// <inheritdoc />
        public void Attach(IArtContext context)
        {
            // ViewManager doesn't subscribe to events from any other managers.
        }

        /// <inheritdoc />
        public void Detach(IArtContext context)
        {
            // ViewManager doesn't subscribe to events from any other managers.
        }

        public Vector2 FromArtSpace(Vector2 v) { return Vector2.Transform(v, Transform); }

        public RectangleF FromArtSpace(RectangleF v) { return MathUtils.Bounds(v, Transform); }

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

        public Vector2 ToArtSpace(Vector2 v) { return Vector2.Transform(v, MathUtils.Invert(Transform)); }

        public RectangleF ToArtSpace(RectangleF v)
        {
            return MathUtils.Bounds(v, MathUtils.Invert(Transform));
        }

        public IArtContext Context { get; }

        public Document Document
        {
            get => Get<Document>();
            set
            {
                Set(value);
                RaisePropertyChanged(nameof(Root));
                value.Updated -= OnDocumentUpdated;
                value.Updated += OnDocumentUpdated;
            }
        }

        public Vector2 Pan
        {
            get => Get<Vector2>();
            set
            {
                Set(Vector2.Clamp(value, -Document.Bounds.Size, Document.Bounds.Size));
                RaisePropertyChanged(nameof(Transform));
            }
        }

        public IContainerLayer Root
        {
            get => Document.Root;
            set => Document.Root = value;
        }

        public Matrix3x2 Transform => Matrix3x2.CreateScale(Zoom) * Matrix3x2.CreateTranslation(Pan);

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