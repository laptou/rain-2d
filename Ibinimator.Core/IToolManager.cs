using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;

using Ibinimator.Core.Model;

namespace Ibinimator.Core
{
    public interface IToolManager : IArtContextManager
    {
        ITool    Tool { get; }
        ToolType Type { get; set; }

        event EventHandler<IBrushInfo> FillUpdated;
        event EventHandler<IPenInfo>   StrokeUpdated;

        bool KeyDown(Key key, ModifierState state);
        bool KeyUp(Key   key, ModifierState state);

        bool MouseDown(Vector2 pos, ModifierState state);
        bool MouseMove(Vector2 pos, ModifierState state);
        bool MouseUp(Vector2   pos, ModifierState state);
        void RaiseFillUpdate();

        void RaiseStatus(Status status);
        void RaiseStrokeUpdate();
        bool TextInput(string text);
    }
}