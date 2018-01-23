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

        public BrushManager(IArtContext artContext)
        {
            Context = artContext;
            _brushHistory = new Stack<IBrushInfo>();
        }

        #region IBrushManager Members

        public void Apply(IBrushInfo fill) => Context.ToolManager.Tool.ApplyFill(fill);

        public void Apply(IPenInfo pen) => Context.ToolManager.Tool.ApplyStroke(pen);

        /// <inheritdoc />
        public void Attach(IArtContext context)
        {
        }

        /// <inheritdoc />
        public void Detach(IArtContext context)
        {
        }

        public (IBrushInfo Fill, IPenInfo Stroke) Query()
        {
            var fill = Context.ToolManager.Tool.ProvideFill();
            var stroke = Context.ToolManager.Tool.ProvideStroke();

            var top = _brushHistory.Count == 0 ? null : _brushHistory.Peek();

            if (fill != null && top != fill)
                _brushHistory.Push(fill);

            if (stroke?.Brush != null && top != stroke.Brush)
                _brushHistory.Push(stroke.Brush);

            return (fill, stroke);
        }

        public IReadOnlyCollection<IBrushInfo> BrushHistory => _brushHistory;

        public IArtContext Context { get; }

        #endregion
    }
}