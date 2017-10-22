using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Renderer
{
    public interface ICacheManager : IArtContextManager
    {
        void Bind(Document root);
        void BindLayer(ILayer layer);
        bool ClearResource(ILayer layer, int id);

        RectangleF GetAbsoluteBounds(ILayer layer);

        IBitmap GetBitmap(string key);
        RectangleF GetBounds(ILayer layer);
        IBrush GetBrush(string key);
        IBrush GetFill(IFilledLayer layer);
        IGeometry GetGeometry(IGeometricLayer layer);
        RectangleF GetRelativeBounds(ILayer layer);
        T GetResource<T>(ILayer layer, int id) where T : IDisposable;
        IEnumerable<(int id, T resource)> GetResources<T>(ILayer layer) where T : IDisposable;
        IPen GetStroke(IStrokedLayer layer);
        ITextLayout GetTextLayout(ITextLayer text);

        void LoadBitmaps(RenderContext target);
        void LoadBrushes(RenderContext target);
        QuickLock Lock();
        void ResetAll();
        void ResetDeviceResources();
        void SetResource<T>(ILayer layer, int id, T resource) where T : IDisposable;
    }

    public class QuickLock : IDisposable
    {
        private readonly object _obj;

        public QuickLock(object obj)
        {
            _obj = obj;

            Monitor.Enter(_obj);
        }

        #region IDisposable Members

        public void Dispose() { Monitor.Exit(_obj); }

        #endregion
    }
}