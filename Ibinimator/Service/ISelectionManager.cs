using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using SharpDX;
using SharpDX.Direct2D1;
using Layer = Ibinimator.Model.Layer;

namespace Ibinimator.Service
{
    public interface ISelectionManager : IArtViewManager, INotifyPropertyChanged
    {
        Bitmap Cursor { get; set; }
        Group Root { get; }
        IList<Layer> Selection { get; }
        RectangleF SelectionBounds { get; }
        float SelectionRotation { get; }
        float SelectionShear { get; }
        event EventHandler Updated;
        void ClearSelection();
        Vector2 FromSelectionSpace(Vector2 v);
        void MouseDown(Vector2 pos);
        void MouseMove(Vector2 pos);
        void MouseUp(Vector2 pos);
        void Render(RenderTarget target, ICacheManager cache);

        Vector2 ToSelectionSpace(Vector2 v);

        void Transform(Vector2 scale, Vector2 translate, float rotate, float shear, Vector2 origin);
        void Update(bool reset);
    }
}