using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;

namespace Ibinimator.View.Control
{
    public class ArtContext : PropertyChangedBase, IArtContext
    {
        private readonly ArtView _artView;

        private Status _status;
        public ArtContext(ArtView artView) { _artView = artView; }

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
                BrushManager = (IBrushManager) manager;

            if (interfaces.Contains(typeof(ICacheManager)))
            {
                CacheManager?.ResetAll();

                CacheManager = (ICacheManager) manager;

                if (_artView.RenderContext != null)
                {
                    CacheManager.LoadBrushes(_artView.RenderContext);
                    CacheManager.LoadBitmaps(_artView.RenderContext);

                    if (ViewManager?.Root != null)
                        CacheManager.Bind(ViewManager.Document);
                }
            }

            if (interfaces.Contains(typeof(IHistoryManager)))
                HistoryManager = (IHistoryManager) manager;

            if (interfaces.Contains(typeof(ISelectionManager)))
                SelectionManager = (ISelectionManager) manager;

            if (interfaces.Contains(typeof(IToolManager)))
                ToolManager = (IToolManager) manager;

            if (interfaces.Contains(typeof(IViewManager)))
            {
                ViewManager = (IViewManager) manager;
//                ViewManager.DocumentUpdated +=
//                    (s, e) => CacheManager?.Bind(ViewManager.Document);

                CacheManager?.ResetDeviceResources();
                if (ViewManager?.Root != null)
                    CacheManager?.Bind(ViewManager.Document);
            }

            InvalidateSurface();
        }

        #region IArtContext Members

        public event EventHandler StatusChanged;

        public void InvalidateSurface() { _artView.InvalidateSurface(null); }

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
    }
}