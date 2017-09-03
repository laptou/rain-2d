using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using Layer = Ibinimator.Model.Layer;

namespace Ibinimator.Service
{
    public interface ICacheManager : IArtViewManager
    {
        void Bind(Document root);
        Brush BindBrush(ILayer shape, BrushInfo brush);
        void BindLayer(ILayer layer);

        RectangleF GetAbsoluteBounds(ILayer layer);
        RectangleF GetRelativeBounds(ILayer layer);
        RectangleF GetBounds(ILayer layer);

        Bitmap GetBitmap(string key);
        Brush GetBrush(string key);
        Brush GetFill(IFilledLayer layer);
        Geometry GetGeometry(IGeometricLayer layer);
        TextLayout GetTextLayout(ITextLayer text);
        (Brush brush, float width, StrokeStyle style) GetStroke(IStrokedLayer layer, RenderTarget target);
        void LoadBitmaps(RenderTarget target);
        void LoadBrushes(RenderTarget target);
        void ResetAll();
        void ResetLayerCache();
    }
}