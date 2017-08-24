using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using SharpDX;
using SharpDX.Direct2D1;
using Layer = Ibinimator.Model.Layer;

namespace Ibinimator.Service
{
    public interface ICacheManager : IArtViewManager, INotifyPropertyChanged
    {
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
        void UpdateLayer(Layer layer, string property);
        void BindRoot(Layer root);
    }
}