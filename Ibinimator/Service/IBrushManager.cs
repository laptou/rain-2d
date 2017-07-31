using Ibinimator.Model;
using Ibinimator.View.Control;
using SharpDX.Direct2D1;

namespace Ibinimator.Service
{
    public interface IBrushManager
    {
        ArtView ArtView { get; }
        BrushInfo Fill { get; set; }
        BrushInfo Stroke { get; set; }
        StrokeStyleProperties1 StrokeStyle { get; set; }
        float StrokeWidth { get; set; }
    }
}