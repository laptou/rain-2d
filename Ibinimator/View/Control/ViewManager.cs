using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ibinimator.Model;
using SharpDX;

namespace Ibinimator.View.Control
{
    public class ViewManager : Model.Model, IViewManager
    {
        public ViewManager(ArtView artView)
        {
            ArtView = artView;
            Zoom = 1;
        }

        public ArtView ArtView { get; }

        public float Zoom { get => Get<float>(); set { Set(value); Update(); } }
        public Vector2 Pan { get => Get<Vector2>(); set { Set(value); Update(); } }
        public Matrix3x2 Transform { get => Get<Matrix3x2>(); set => Set(value); }

        public Layer Root
        {
            get => Get<Layer>();
            set
            {
                Set(value);
            }
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
