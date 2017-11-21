using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.View.Control;

namespace Ibinimator.Service
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

        private void Update() { Transform = Matrix3x2.CreateTranslation(Pan) * Matrix3x2.CreateScale(Zoom); }

        #region IViewManager Members

        public event PropertyChangedEventHandler DocumentUpdated;

        public Vector2 FromArtSpace(Vector2 v) { return Vector2.Transform(v, Transform); }

        public RectangleF FromArtSpace(RectangleF v) { return MathUtils.Bounds(v, Transform); }

        public void Render(RenderContext target, ICacheManager cache)
        {
            using (var brush = target.CreateBrush(new Color(0.9f)))
            using (var pen = target.CreatePen(1, brush))
                target.DrawRectangle(Document.Bounds, pen);

            using (var brush = target.CreateBrush(new Color(1f)))
                target.FillRectangle(Document.Bounds, brush);
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
                Update();
            }
        }

        public Group Root
        {
            get => Document.Root;
            set => Document.Root = value;
        }

        public Matrix3x2 Transform
        {
            get => Get<Matrix3x2>();
            private set => Set(value);
        }

        public float Zoom
        {
            get => Get<float>();
            set
            {
                Set(MathUtils.Clamp(1e-4f, 1e4f, value));
                Update();
            }
        }

        #endregion
    }
}