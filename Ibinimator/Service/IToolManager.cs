using System.ComponentModel;
using Ibinimator.View.Control;
using SharpDX;

namespace Ibinimator.Service
{
    public interface IToolManager : INotifyPropertyChanged
    {
        ArtView ArtView { get; }
        ITool Tool { get; }

        void MouseDown(Vector2 pos);
        void MouseMove(Vector2 pos);
        void MouseUp(Vector2 pos);

        void SetTool(ToolType type);
    }

    public interface ITool : INotifyPropertyChanged
    {
        IToolManager Manager { get; }
        ToolType Type { get; }
        string Status { get; }

        void MouseDown(Vector2 pos);
        void MouseMove(Vector2 pos);
        void MouseUp(Vector2 pos);
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