using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public interface IArtContext
    {
        IBrushManager BrushManager { get; }

        ICacheManager CacheManager { get; }

        IHistoryManager HistoryManager { get; }
        RenderContext RenderContext { get; }

        ISelectionManager SelectionManager { get; }

        IToolManager ToolManager { get; }

        IViewManager ViewManager { get; }

        void InvalidateSurface();
    }
}