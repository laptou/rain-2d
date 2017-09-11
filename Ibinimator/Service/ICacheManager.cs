using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace Ibinimator.Service
{
    public interface ICacheManager : IArtViewManager
    {
        void Bind(Document root);
        Brush BindBrush(ILayer shape, BrushInfo brush);
        void BindLayer(ILayer layer);

        RectangleF GetAbsoluteBounds(ILayer layer);

        Bitmap GetBitmap(string key);
        RectangleF GetBounds(ILayer layer);
        Brush GetBrush(string key);
        T GetResource<T>(ILayer layer, long id) where T : IDisposable;
        IEnumerable<(long, T)> GetResources<T>(ILayer layer) where T : IDisposable;
        bool ClearResource<T>(ILayer layer, long id) where T : IDisposable;
        Brush GetFill(IFilledLayer layer);
        GeometryRealization GetFillGeometry(IGeometricLayer layer);
        Geometry GetGeometry(IGeometricLayer layer);
        RectangleF GetRelativeBounds(ILayer layer);
        Stroke GetStroke(IStrokedLayer layer);
        GeometryRealization GetStrokeGeometry(IGeometricLayer layer);
        TextLayout GetTextLayout(ITextLayer text);
        void LoadBitmaps(RenderTarget target);
        void LoadBrushes(RenderTarget target);
        void ResetAll();
        void ResetLayerCache();
    }
}