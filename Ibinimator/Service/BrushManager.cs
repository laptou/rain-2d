using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Service
{
    public class BrushManager : Core.Model.Model, IBrushManager
    {
        private readonly Stack<IBrushInfo> _brushHistory;

        public BrushManager(
            IArtContext       artContext,
            ISelectionManager selectionManager,
            IHistoryManager   historyManager,
            IToolManager      toolManager)
        {
            Context = artContext;
            _brushHistory = new Stack<IBrushInfo>();

            selectionManager.Updated += OnUpdated;
            historyManager.Traversed += (s, e) => OnUpdated(s, null);
            toolManager.StrokeUpdated += (s, e) => OnUpdated(s, null);
            toolManager.FillUpdated += (s, e) => OnUpdated(s, null);

            Fill = new SolidColorBrushInfo(new Color(0, 0, 0));
            Stroke = null;
        }

        private void OnUpdated(object sender, EventArgs args) { Query(); }

        #region IBrushManager Members

        public void ApplyFill() { Context.ToolManager.Tool.ApplyFill(Fill); }

        public void ApplyStroke() { Context.ToolManager.Tool.ApplyStroke(Stroke); }

        public void Query()
        {
            var (oldFill, oldStroke) = (Fill, Stroke);
            Fill = Context.ToolManager.Tool.ProvideFill();
            Stroke = Context.ToolManager.Tool.ProvideStroke();
            
            var top = _brushHistory.Count == 0 ? null : _brushHistory.Peek();

            if (oldFill != null && top != oldFill)
                _brushHistory.Push(Fill);

            if (oldStroke?.Brush != null && top != oldStroke.Brush)
                _brushHistory.Push(oldStroke.Brush);
        }

        public IReadOnlyCollection<IBrushInfo> BrushHistory => _brushHistory;

        public IArtContext Context { get; }

        public IBrushInfo Fill
        {
            get => Get<IBrushInfo>();
            set => Set(value);
        }

        public IPenInfo Stroke
        {
            get => Get<IPenInfo>();
            set => Set(value);
        }

        #endregion
    }
}