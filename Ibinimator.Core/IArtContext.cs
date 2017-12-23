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

        event ArtContextEventHandler<FocusEvent> GainedFocus;

        event ArtContextEventHandler<KeyboardEvent> KeyDown;

        event ArtContextEventHandler<KeyboardEvent> KeyUp;

        event ArtContextEventHandler<FocusEvent> LostFocus;

        event ArtContextEventHandler<ClickEvent> MouseDown;

        event ArtContextEventHandler<PointerEvent> MouseMove;

        event ArtContextEventHandler<ClickEvent> MouseUp;

        event EventHandler StatusChanged;

        event ArtContextEventHandler<TextEvent> Text;

        void InvalidateRender();
    }

    public delegate void ArtContextEventHandler<in T>(IArtContext sender, T evt) where T : IInputEvent;
}