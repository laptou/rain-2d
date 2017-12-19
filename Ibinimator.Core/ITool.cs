using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;

using Ibinimator.Core.Model;

namespace Ibinimator.Core
{
    public interface ITool : INotifyPropertyChanged, IDisposable
    {
        string Cursor       { get; }
        float  CursorRotate { get; }

        IToolManager Manager { get; }
        ToolOptions  Options { get; }

        ToolType Type { get; }

        void Render(RenderContext target, ICacheManager cache, IViewManager view);

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

        #region Events

        bool KeyDown(Key key, ModifierState modifiers);
        bool KeyUp(Key   key, ModifierState modifiers);

        bool MouseDown(Vector2 pos, ModifierState state);
        bool MouseMove(Vector2 pos, ModifierState state);
        bool MouseUp(Vector2   pos, ModifierState state);

        bool TextInput(string text);

        #endregion
    }
}