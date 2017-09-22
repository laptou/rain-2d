using Ibinimator.Direct2D;
using Ibinimator.Service;
using SharpDX.Direct2D1;

namespace Ibinimator.View.Control
{
    public interface IArtView
    {
        IBrushManager BrushManager { get; }
        ICacheManager CacheManager { get; }
        IHistoryManager HistoryManager { get; }
        RenderTarget RenderTarget { get; }
        ISelectionManager SelectionManager { get; }
        IToolManager ToolManager { get; }
        IViewManager ViewManager { get; }

        void InvalidateSurface();
        void SetManager<T>(T manager) where T : IArtViewManager;
    }
}