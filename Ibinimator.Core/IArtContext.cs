using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
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

        Status Status { get; set; }

        event EventHandler StatusChanged;

        void InvalidateSurface();
    }
}