using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Service
{
    public class BrushManager : Model, IBrushManager
    {
        private bool _selecting;

        public BrushManager(IArtContext artContext, ISelectionManager selectionManager, IHistoryManager historyManager)
        {
            Context = artContext;

            selectionManager.Updated += OnUpdated;

            historyManager.Traversed += (s, e) => OnUpdated(s, null);

            Fill = new SolidColorBrushInfo(new Color(0, 0, 0));
            Stroke = new PenInfo
            {
                Brush = new SolidColorBrushInfo(new Color(0, 0, 0))
            };
        }

        private void OnUpdated(object sender, EventArgs args)
        {
            Fill = Context.ToolManager.Tool.ProvideFill();
            Stroke = Context.ToolManager.Tool.ProvideStroke();
        }

        #region IBrushManager Members

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