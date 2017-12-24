using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Core.Input;
using Ibinimator.Core.Model;

namespace Ibinimator.View.Control
{
    public class ArtContext : PropertyChangedBase, IArtContext
    {
        private readonly ArtView _artView;
        private          Status  _status;

        public ArtContext(ArtView artView)
        {
            _artView = artView;
            _artView.RenderTargetCreated += ArtViewOnRenderTargetCreated;
        }

        private void ArtViewOnRenderTargetCreated(object sender, EventArgs eventArgs)
        {
            CacheManager?.ResetDeviceResources();
            if (ViewManager?.Root != null)
                CacheManager?.BindLayer(ViewManager.Document.Root);
        }

        public void SetManager<T>(T manager) where T : IArtContextManager
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));

            var managerInterfaces =
                typeof(T).FindInterfaces(
                              (type, criteria) => typeof(IArtContextManager).IsAssignableFrom(type),
                              null)
                         .Concat(new[] {typeof(T)});

            var interfaces = managerInterfaces.ToList();

            if (interfaces.Contains(typeof(IBrushManager)))
            {
                BrushManager?.Detach(this);
                BrushManager = (IBrushManager) manager;
                BrushManager.Attach(this);
            }

            if (interfaces.Contains(typeof(ICacheManager)))
            {
                CacheManager?.ResetResources();
                CacheManager?.Detach(this);

                CacheManager = (ICacheManager) manager;
                CacheManager.Attach(this);

                if (_artView.RenderContext != null)
                {
                    CacheManager.LoadBrushes(_artView.RenderContext);
                    CacheManager.LoadBitmaps(_artView.RenderContext);

                    if (ViewManager?.Root != null)
                        CacheManager?.BindLayer(ViewManager.Document.Root);
                }
            }

            if (interfaces.Contains(typeof(IHistoryManager)))
            {
                HistoryManager?.Detach(this);
                HistoryManager = (IHistoryManager) manager;
                HistoryManager.Attach(this);
            }

            if (interfaces.Contains(typeof(ISelectionManager)))
            {
                SelectionManager?.Detach(this);
                SelectionManager = (ISelectionManager) manager;
                SelectionManager.Attach(this);
            }

            if (interfaces.Contains(typeof(IToolManager)))
            {
                ToolManager?.Detach(this);
                ToolManager = (IToolManager) manager;
                ToolManager.Attach(this);
            }

            if (interfaces.Contains(typeof(IViewManager)))
            {
                ViewManager?.Detach(this);
                ViewManager = (IViewManager) manager;
                ViewManager.Attach(this);
            }

            InvalidateRender();
        }

        #region IArtContext Members

        public void InvalidateRender() { _artView.InvalidateSurface(); }

        public RenderContext RenderContext => _artView.RenderContext;

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

        public void RaiseMouseUp(ClickEvent evt) { MouseUp?.Invoke(this, evt); }

        public void RaiseMouseDown(ClickEvent evt) { MouseUp?.Invoke(this, evt); }

        public void RaiseMouseMove(PointerEvent evt) { MouseMove?.Invoke(this, evt); }

        public void RaiseKeyUp(KeyboardEvent evt) { KeyUp?.Invoke(this, evt); }

        public void RaiseKeyDown(KeyboardEvent evt) { KeyDown?.Invoke(this, evt); }

        public void RaiseLostFocus(FocusEvent evt) { LostFocus?.Invoke(this, evt); }

        public void RaiseGainedFocus(FocusEvent evt) { GainedFocus?.Invoke(this, evt); }

        public void RaiseText(TextEvent evt) { Text?.Invoke(this, evt); }

        /// <inheritdoc />
        public event ArtContextEventHandler<ClickEvent> MouseUp;

        public event EventHandler StatusChanged;

        /// <inheritdoc />
        public event ArtContextEventHandler<TextEvent> Text;

        /// <inheritdoc />
        public event ArtContextEventHandler<FocusEvent> GainedFocus;

        /// <inheritdoc />
        public event ArtContextEventHandler<KeyboardEvent> KeyDown;

        /// <inheritdoc />
        public event ArtContextEventHandler<KeyboardEvent> KeyUp;

        /// <inheritdoc />
        public event ArtContextEventHandler<FocusEvent> LostFocus;

        /// <inheritdoc />
        public event ArtContextEventHandler<ClickEvent> MouseDown;

        /// <inheritdoc />
        public event ArtContextEventHandler<PointerEvent> MouseMove;

        #endregion
    }
}