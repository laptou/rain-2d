using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public interface IArtContext
    {
        RenderContext RenderContext { get; }

        ICacheManager CacheManager { get; }

        ISelectionManager SelectionManager { get; }

        IHistoryManager HistoryManager { get; }

        IViewManager ViewManager { get; }

        IBrushManager BrushManager { get; }

        IToolManager ToolManager { get; }

        void InvalidateSurface();
    }
}