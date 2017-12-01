using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Core.Model;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Renderer
{
    public interface IToolManager : IArtContextManager
    {
        ITool Tool { get; }
        ToolType Type { get; set; }

        bool KeyDown(Key key, ModifierKeys modifiers);
        bool KeyUp(Key key, ModifierKeys modifiers);

        bool MouseDown(Vector2 pos);
        bool MouseMove(Vector2 pos);
        bool MouseUp(Vector2 pos);
        bool TextInput(string text);

        void RaiseStatus(Status status);
        void RaiseFillUpdate();
        void RaiseStrokeUpdate();


        event EventHandler<BrushInfo> FillUpdated;
        event EventHandler<BrushInfo> StrokeUpdated;
    }
}