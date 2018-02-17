using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Paint;

namespace Ibinimator.Core.Model.Text
{
    public interface IAttachment
    {
        void Attach(IArtContext context);
        void Detach(IArtContext context);
    }

    public interface ITool : INotifyPropertyChanged, IRenderable, IAttachment
    {
        string Cursor { get; }
        float CursorRotate { get; }

        IToolManager Manager { get; }
        ToolOptions Options { get; }
        ToolType Type { get; }

        #region Fill and Stroke

        /// <summary>
        ///     Applies the given brush to the current selection of the tool.
        /// </summary>
        /// <param name="brush">The brush to be applied.</param>
        void ApplyFill(IBrushInfo brush);

        /// <summary>
        ///     Applies the given stroke to the current selection of the tool.
        /// </summary>
        /// <param name="pen">The stroke to be applied.</param>
        void ApplyStroke(IPenInfo pen);

        /// <summary>
        ///     Queries the fill of the current selection of the tool.
        /// </summary>
        /// <returns>The current brush of the selection.</returns>
        IBrushInfo ProvideFill();

        /// <summary>
        ///     Queries the stroke of the current selection of the tool.
        /// </summary>
        /// <returns>The current stroke of the selection.</returns>
        IPenInfo ProvideStroke();

        #endregion
    }
}