using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Model;

namespace Ibinimator.Core
{
    public interface ICacheManager : IArtContextManager
    {
        event EventHandler<ILayer> BoundsChanged;
        
        void BindLayer(ILayer layer);

        RectangleF GetAbsoluteBounds(ILayer layer);
        IBitmap GetBitmap(string key);
        RectangleF GetBounds(ILayer layer);
        IBrush GetBrush(string key);
        IBrush GetFill(IFilledLayer layer);
        IGeometry GetGeometry(IGeometricLayer layer);
        RectangleF GetRelativeBounds(ILayer layer);

        IPen GetStroke(IStrokedLayer layer);
        ITextLayout GetTextLayout(ITextLayer text);

        void LoadBitmaps(RenderContext target);
        void LoadBrushes(RenderContext target);

        void ResetAll();
        void ResetDeviceResources();
    }
}