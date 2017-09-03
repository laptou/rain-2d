using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.View.Control;
using SharpDX;

namespace Ibinimator.Service
{
    public class ViewManager : Model.Model, IViewManager
    {
        public ViewManager(ArtView artView)
        {
            ArtView = artView;
            Document = new Document();
            Zoom = 1;
        }

        #region IViewManager Members

        public ArtView ArtView { get; }

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

        public event PropertyChangedEventHandler DocumentUpdated;

        public Vector2 FromArtSpace(Vector2 v)
        {
            return v / Zoom - Pan;
        }

        public RectangleF FromArtSpace(RectangleF v)
        {
            v.Top /= Zoom;
            v.Left /= Zoom;
            v.Bottom /= Zoom;
            v.Right /= Zoom;
            v.Location -= Pan;
            return v;
        }

        public Vector2 Pan
        {
            get => Get<Vector2>();
            set
            {
                Set(value);
                Update();
            }
        }

        public Group Root
        {
            get => Document.Root;
            set => Document.Root = value;
        }

        public Vector2 ToArtSpace(Vector2 v)
        {
            return (v + Pan) * Zoom;
        }

        public RectangleF ToArtSpace(RectangleF v)
        {
            v.Location += Pan;
            v.Top *= Zoom;
            v.Left *= Zoom;
            v.Bottom *= Zoom;
            v.Right *= Zoom;
            return v;
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
                Set(value);
                Update();
            }
        }

        #endregion

        private void OnDocumentUpdated(object sender, PropertyChangedEventArgs e)
        {
            DocumentUpdated?.Invoke(sender, e);
        }

        private void Update()
        {
            Transform = Matrix3x2.Translation(Pan) * Matrix3x2.Scaling(Zoom);
        }
    }
}