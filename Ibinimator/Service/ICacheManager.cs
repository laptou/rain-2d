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
        bool ClearResource(ILayer layer, int id);

        RectangleF GetAbsoluteBounds(ILayer layer);

        Bitmap GetBitmap(string key);
        RectangleF GetBounds(ILayer layer);
        Brush GetBrush(string key);
        Brush GetFill(IFilledLayer layer);
        Geometry GetGeometry(IGeometricLayer layer);
        GeometryRealization GetGeometryRealization(IGeometricLayer layer);
        RectangleF GetRelativeBounds(ILayer layer);
        T GetResource<T>(ILayer layer, int id) where T : IDisposable;
        IEnumerable<(int id, T resource)> GetResources<T>(ILayer layer) where T : IDisposable;
        Stroke GetStroke(IStrokedLayer layer);
        TextLayout GetTextLayout(ITextLayer text);
        void LoadBitmaps(RenderTarget target);
        void LoadBrushes(RenderTarget target);
        void ResetAll();
        void ResetDeviceResources();
        void SetResource<T>(ILayer layer, int id, T resource) where T : IDisposable;
    }
}