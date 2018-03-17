using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core;
using Rain.Core.Input;
using Rain.Core.Model;
using Rain.Core.Model.Effects;
using Rain.Core.Model.Imaging;
using Rain.Core.Model.Text;
using Rain.Native;
using Rain.Renderer.Direct2D;
using Rain.Renderer.WIC;

namespace Rain.View.Control
{
    public class ArtContext : PropertyChangedBase, IArtContext
    {
        private readonly ArtView _artView;
        private          Status  _status;

        public ArtContext(ArtView artView) { _artView = artView; }

        #region IArtContext Members

        /// <inheritdoc />
        public ICaret CreateCaret(int width, int height)
        {
            if (WindowHelper.GetFocus() == _artView.Handle)
                return new Caret(_artView.Handle,
                                 width, height);

            return null;
        }

        

        public void Invalidate() { _artView.InvalidateSurface(); }

        public void SetManager<T>(T manager) where T : IArtContextManager
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            if (manager is IBrushManager brushManager)
            {
                BrushManager?.Detach(this);
                BrushManager = brushManager;
                BrushManager.Attach(this);
            }

            if (manager is ICacheManager cacheManager)
            {
                CacheManager?.ReleaseResources();
                CacheManager?.Detach(this);

                CacheManager = cacheManager;
                CacheManager.Attach(this);

                if (_artView.RenderContext != null)
                {
                    CacheManager.LoadApplicationResources(_artView.RenderContext);

                    if (ViewManager?.Root != null)
                        CacheManager?.BindLayer(ViewManager.Document.Root);
                }
            }

            if (manager is IHistoryManager historyManager)
            {
                HistoryManager?.Detach(this);
                HistoryManager = historyManager;
                HistoryManager.Attach(this);
            }

            if (manager is ISelectionManager selectionManager)
            {
                SelectionManager?.Detach(this);
                SelectionManager = selectionManager;
                SelectionManager.Attach(this);
            }

            if (manager is IToolManager toolManager)
            {
                ToolManager?.Detach(this);
                ToolManager = toolManager;
                ToolManager.Attach(this);
            }

            if (manager is IViewManager viewManager)
            {
                ViewManager?.Detach(this);
                ViewManager = viewManager;
                ViewManager.Attach(this);
            }

            Invalidate();
        }

        public IRenderContext RenderContext => _artView.RenderContext;

        /// <inheritdoc />
        public ResourceContext ResourceContext { get; } = new WICResourceContext();

        public Status Status
        {
            get => _status;
            set
            {
                _status = value;
                StatusChanged?.Invoke(this, null);
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Managers

        public IBrushManager BrushManager { get; set; }
        public ICacheManager CacheManager { get; set; }
        public IHistoryManager HistoryManager { get; set; }
        public ISelectionManager SelectionManager { get; set; }
        public IToolManager ToolManager { get; set; }
        public IViewManager ViewManager { get; set; }

        #endregion

        #region Events

        public void RaiseAttached(IArtContextManager mgr) { ManagerAttached?.Invoke(mgr, null); }

        public void RaiseDetached(IArtContextManager mgr) { ManagerDetached?.Invoke(mgr, null); }

        public void RaiseMouseUp(ClickEvent evt) { MouseUp?.Invoke(this, evt); }

        public void RaiseMouseDown(ClickEvent evt) { MouseDown?.Invoke(this, evt); }

        public void RaiseMouseMove(PointerEvent evt) { MouseMove?.Invoke(this, evt); }

        public void RaiseKeyUp(KeyboardEvent evt) { KeyUp?.Invoke(this, evt); }

        public void RaiseKeyDown(KeyboardEvent evt) { KeyDown?.Invoke(this, evt); }

        public void RaiseLostFocus(FocusEvent evt) { LostFocus?.Invoke(this, evt); }

        public void RaiseGainedFocus(FocusEvent evt) { GainedFocus?.Invoke(this, evt); }

        public void RaiseText(TextEvent evt) { Text?.Invoke(this, evt); }

        /// <inheritdoc />
        public event ArtContextInputEventHandler<ClickEvent> MouseUp;

        /// <inheritdoc />
        public event EventHandler ManagerDetached;

        public event EventHandler StatusChanged;

        /// <inheritdoc />
        public event ArtContextInputEventHandler<TextEvent> Text;

        /// <inheritdoc />
        public event EventHandler ManagerAttached;

        /// <inheritdoc />
        public event ArtContextInputEventHandler<FocusEvent> GainedFocus;

        /// <inheritdoc />
        public event ArtContextInputEventHandler<KeyboardEvent> KeyDown;

        /// <inheritdoc />
        public event ArtContextInputEventHandler<KeyboardEvent> KeyUp;

        /// <inheritdoc />
        public event ArtContextInputEventHandler<FocusEvent> LostFocus;

        /// <inheritdoc />
        public event ArtContextInputEventHandler<ClickEvent> MouseDown;

        /// <inheritdoc />
        public event ArtContextInputEventHandler<PointerEvent> MouseMove;

        #endregion
    }
}