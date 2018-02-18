using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Input;

namespace Ibinimator.Core
{
    public interface IArtContext
    {
        IBrushManager BrushManager { get; }

        ICacheManager CacheManager { get; }

        IHistoryManager HistoryManager { get; }

        RenderContext RenderContext { get; }

        ISelectionManager SelectionManager { get; }

        Status Status { get; set; }

        IToolManager ToolManager { get; }

        IViewManager ViewManager { get; }

        event ArtContextInputEventHandler<FocusEvent> GainedFocus;

        event ArtContextInputEventHandler<KeyboardEvent> KeyDown;

        event ArtContextInputEventHandler<KeyboardEvent> KeyUp;

        event ArtContextInputEventHandler<FocusEvent> LostFocus;

        event EventHandler ManagerAttached;

        event EventHandler ManagerDetached;

        event ArtContextInputEventHandler<ClickEvent> MouseDown;

        event ArtContextInputEventHandler<PointerEvent> MouseMove;

        event ArtContextInputEventHandler<ClickEvent> MouseUp;

        event EventHandler StatusChanged;

        event ArtContextInputEventHandler<TextEvent> Text;

        T Create<T>(params object[] parameters) where T : class;

        void InvalidateRender();

        void RaiseAttached(IArtContextManager mgr);

        void RaiseDetached(IArtContextManager mgr);

        void SetManager<T>(T manager) where T : IArtContextManager;
    }

    public delegate void ArtContextInputEventHandler<in T>(IArtContext sender, T evt)
        where T : IInputEvent;
}