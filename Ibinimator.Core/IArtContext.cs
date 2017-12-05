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

    public class Status
    {
        public Status(StatusType type, string message)
        {
            Type = type;
            Message = message;
        }

        public Status(float percentage)
        {
            Percentage = percentage;
            Type = StatusType.Progress;
        }

        public float Percentage { get; } = -1;

        public StatusType Type { get; }

        public string Message { get; }

        public enum StatusType
        {
            Info,
            Warning,
            Error,
            Success,
            Progress
        }
    }
}