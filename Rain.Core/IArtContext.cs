using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Input;
using Rain.Core.Model.Imaging;
using Rain.Core.Model.Text;

namespace Rain.Core
{
    public interface IArtContext
    {
        IBrushManager BrushManager { get; }

        ICacheManager CacheManager { get; }

        IHistoryManager HistoryManager { get; }

        IRenderContext RenderContext { get; }

        ResourceContext ResourceContext { get; }

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

        ICaret CreateCaret(int width, int height);
        void Invalidate();

        void RaiseAttached(IArtContextManager mgr);

        void RaiseDetached(IArtContextManager mgr);

        void SetManager<T>(T manager) where T : IArtContextManager;
    }

    public abstract class ResourceContext
    {
        public abstract IImage LoadImageFromFilename(string filename);
        public abstract IImage LoadImageFromStream(Stream stream);
    }

    public delegate void ArtContextInputEventHandler<in T>(IArtContext sender, T evt) where T : IInputEvent;
}