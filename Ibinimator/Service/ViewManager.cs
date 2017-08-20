using System.ComponentModel;
using Ibinimator.Model;
using SharpDX;
using Ibinimator.View.Control;

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

        public ArtView ArtView { get; }

        public float Zoom { get => Get<float>(); set { Set(value); Update(); } }
        public Vector2 Pan { get => Get<Vector2>(); set { Set(value); Update(); } }
        public Matrix3x2 Transform { get => Get<Matrix3x2>(); set => Set(value); }

        public Layer Root
        {
            get => Document.Root;
            set => Document.Root = value;
        }

        public Document Document
        {
            get => Get<Document>();
            set => Set(value);
        }

        private void RootPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            LayerUpdated?.Invoke(sender, propertyChangedEventArgs);
        }

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

        public event PropertyChangedEventHandler LayerUpdated;

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

        private void Update()
        {
            Transform = Matrix3x2.Translation(Pan) * Matrix3x2.Scaling(Zoom);
        }
    }
}
