using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using SharpDX;
using SharpDX.Direct2D1;
using Layer = Ibinimator.Model.Layer;

namespace Ibinimator.Service
{
    public interface ICacheManager : IArtViewManager
    {
        void Bind(Document root);
        Brush BindBrush(Shape shape, BrushInfo brush);
        void BindLayer(Layer layer);

        Bitmap GetBitmap(string key);
        RectangleF GetBounds(Layer layer);
        Brush GetBrush(string key);
        Brush GetFill(Shape layer);
        Geometry GetGeometry(Shape layer);
        (Brush brush, float width, StrokeStyle style) GetStroke(Shape layer, RenderTarget target);
        void LoadBitmaps(RenderTarget target);
        void LoadBrushes(RenderTarget target);
        void ResetAll();
        void ResetLayerCache();
        RectangleF GetAbsoluteBounds(Layer layer);
        RectangleF GetRelativeBounds(Layer layer);
    }
}