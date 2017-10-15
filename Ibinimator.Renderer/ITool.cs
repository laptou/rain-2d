using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Renderer.Model;
using SharpDX.Direct2D1;

namespace Ibinimator.Renderer
{
    public interface ITool : INotifyPropertyChanged, IDisposable
    {
        Bitmap Cursor { get; }
        float CursorRotate { get; }
        IToolManager Manager { get; }
        // ToolOption[] Options { get; }
        string Status { get; }
        ToolType Type { get; }

        void ApplyFill(BrushInfo brush);
        void ApplyStroke(PenInfo pen);

        bool KeyDown(Key key);
        bool KeyUp(Key key);

        bool MouseDown(Vector2 pos);
        bool MouseMove(Vector2 pos);
        bool MouseUp(Vector2 pos);

        void Render(RenderContext target, ICacheManager cacheManager);
    }
}