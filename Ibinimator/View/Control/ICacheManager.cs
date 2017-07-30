using Ibinimator.Model;
using SharpDX;
using SharpDX.Direct2D1;
using System.ComponentModel;

namespace Ibinimator.View.Control
{
    public interface ICacheManager : INotifyPropertyChanged
    {
        ArtView ArtView { get; }

        Brush BindBrush(Shape shape, BrushInfo brush);
        void BindLayer(Model.Layer layer);

        Bitmap GetBitmap(string key);
        RectangleF GetBounds(Model.Layer layer);
        Brush GetBrush(string key);
        Brush GetFill(Shape layer);
        Geometry GetGeometry(Shape layer);
        (Brush brush, float width, StrokeStyle style) GetStroke(Shape layer, RenderTarget target);
        void LoadBitmaps(RenderTarget target);
        void LoadBrushes(RenderTarget target);
        void ResetAll();
        void ResetLayerCache();
        void UpdateLayer(Model.Layer layer, string property);
    }
}