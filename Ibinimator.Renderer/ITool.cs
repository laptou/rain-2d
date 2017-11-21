using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Core.Model;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Renderer
{
    public interface ITool : INotifyPropertyChanged, IDisposable
    {
        string CursorImage { get; }
        float CursorRotate { get; }

        IToolManager Manager { get; }
        IToolOption[] Options { get; }

        ToolType Type { get; }

        void ApplyFill(BrushInfo brush);
        void ApplyStroke(PenInfo pen);


        void Render(RenderContext target, ICacheManager cache, IViewManager view);

        #region Events

        bool KeyDown(Key key, ModifierKeys modifiers);
        bool KeyUp(Key key, ModifierKeys modifiers);

        bool MouseDown(Vector2 pos);
        bool MouseMove(Vector2 pos);
        bool MouseUp(Vector2 pos);

        bool TextInput(string text);

        #endregion
    }

    public interface IToolOption
    {
        string Icon { get; set; }
        object Maximum { get; set; }
        object Minimum { get; set; }
        string Name { get; set; }
        IEnumerable<object> Options { get; set; }
        ToolOptionType Type { get; set; }
        Unit Unit { get; set; }
        object Value { get; set; }

        void SetValue(object value);
    }
}