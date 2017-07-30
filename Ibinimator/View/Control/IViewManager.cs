using Ibinimator.Model;
using SharpDX;
using System.ComponentModel;

namespace Ibinimator.View.Control
{
    public interface IViewManager : INotifyPropertyChanged
    {
        ArtView ArtView { get; }

        float Zoom { get; set; }
        Vector2 Pan { get; set; }
        Matrix3x2 Transform { get; }

        Layer Root { get; set; }

        Vector2 ToArtSpace(Vector2 v);
        RectangleF ToArtSpace(RectangleF v);
        Vector2 FromArtSpace(Vector2 v);
        RectangleF FromArtSpace(RectangleF v);
    }
}