using System.ComponentModel;
using System.Windows.Input;
using Ibinimator.View.Control;
using SharpDX;
using SharpDX.Direct2D1;

namespace Ibinimator.Service
{
    public interface IToolManager : IArtViewManager
    {
        ITool Tool { get; }
        ToolType Type { get; set; }

        void MouseDown(Vector2 pos);
        void MouseMove(Vector2 pos);
        void MouseUp(Vector2 pos);
        void KeyDown(KeyEventArgs keyEventArgs);
        void KeyUp(KeyEventArgs keyEventArgs);
    }

    public interface ITool : INotifyPropertyChanged
    {
        IToolManager Manager { get; }
        ToolType Type { get; }
        string Status { get; }
        Bitmap Cursor { get; }
        float CursorRotate { get; }

        void MouseDown(Vector2 pos);
        void MouseMove(Vector2 pos);
        void MouseUp(Vector2 pos);
        void Render(RenderTarget target, ICacheManager cacheManager);
        void KeyDown(Key key);
        void KeyUp(Key key);
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