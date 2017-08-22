using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using SharpDX;
using SharpDX.Direct2D1;

namespace Ibinimator.Service
{
    public interface IToolManager : IArtViewManager
    {
        ITool Tool { get; }
        ToolType Type { get; set; }
        void KeyDown(KeyEventArgs keyEventArgs);
        void KeyUp(KeyEventArgs keyEventArgs);

        void MouseDown(Vector2 pos);
        void MouseMove(Vector2 pos);
        void MouseUp(Vector2 pos);
    }

    public interface ITool : INotifyPropertyChanged
    {
        Bitmap Cursor { get; }
        float CursorRotate { get; }
        IToolManager Manager { get; }
        string Status { get; }
        ToolType Type { get; }
        void KeyDown(Key key);
        void KeyUp(Key key);

        void MouseDown(Vector2 pos);
        void MouseMove(Vector2 pos);
        void MouseUp(Vector2 pos);
        void Render(RenderTarget target, ICacheManager cacheManager);
    }

    public enum ToolType
    {
        Select,
        Path,
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