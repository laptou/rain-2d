using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ibinimator.Renderer
{
    public interface IToolManager : IArtContextManager
    {
        ITool Tool { get; }
        ToolType Type { get; set; }
        bool KeyDown(KeyEventArgs keyEventArgs);
        bool KeyUp(KeyEventArgs keyEventArgs);

        bool MouseDown(Vector2 pos);
        bool MouseMove(Vector2 pos);
        bool MouseUp(Vector2 pos);
    }
}