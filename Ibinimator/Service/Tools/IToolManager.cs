using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Model;
using SharpDX;
using SharpDX.Direct2D1;

namespace Ibinimator.Service.Tools
{
    public interface IToolManager : IArtViewManager
    {
        ITool Tool { get; }
        ToolType Type { get; set; }
        bool KeyDown(KeyEventArgs keyEventArgs);
        bool KeyUp(KeyEventArgs keyEventArgs);

        bool MouseDown(Vector2 pos);
        bool MouseMove(Vector2 pos);
        bool MouseUp(Vector2 pos);
    }

    public interface ITool : INotifyPropertyChanged, IDisposable
    {
        Bitmap Cursor { get; }
        float CursorRotate { get; }
        IToolManager Manager { get; }
        ToolOption[] Options { get; }
        string Status { get; }
        ToolType Type { get; }

        void ApplyFill(BrushInfo brush);
        void ApplyStroke(BrushInfo brush, StrokeInfo stroke);

        bool KeyDown(Key key);
        bool KeyUp(Key key);

        bool MouseDown(Vector2 pos);
        bool MouseMove(Vector2 pos);
        bool MouseUp(Vector2 pos);

        void Render(RenderTarget target, ICacheManager cacheManager);
    }

    public enum ToolType
    {
        Select,
        Node,
        Pencil,
        Pen,
        Eyedropper,
        Bucket,
        Timeline,
        Text,
        Mask,
        Zoom
    }
}