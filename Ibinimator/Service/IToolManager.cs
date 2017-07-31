using System.ComponentModel;
using Ibinimator.View.Control;
using SharpDX;
using SharpDX.Direct2D1;

namespace Ibinimator.Service
{
    public interface IToolManager : INotifyPropertyChanged
    {
        ArtView ArtView { get; }
        ITool Tool { get; }
        ToolType Type { get; set; }

        void MouseDown(Vector2 pos);
        void MouseMove(Vector2 pos);
        void MouseUp(Vector2 pos);
    }

    public interface ITool : INotifyPropertyChanged
    {
        IToolManager Manager { get; }
        ToolType Type { get; }
        string Status { get; }

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