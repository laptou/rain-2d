using System.Collections.Generic;
using Ibinimator.Model;
using SharpDX;
using SharpDX.Direct2D1;
using System.ComponentModel;
using System;
using Ibinimator.View.Control;

namespace Ibinimator.Service
{
    public interface ISelectionManager : IArtViewManager, INotifyPropertyChanged
    {
        Bitmap Cursor { get; set; }
        Model.Layer Root { get; }
        IList<Model.Layer> Selection { get; }
        RectangleF SelectionBounds { get; }
        float SelectionRotation { get; }
        float SelectionShear { get; }
        event EventHandler Updated;

        void Transform(Vector2 scale, Vector2 translate, float rotate, float shear, Vector2 origin);
        void MouseDown(Vector2 pos);
        void MouseMove(Vector2 pos);
        void MouseUp(Vector2 pos);
        void Render(RenderTarget target, ICacheManager cache);
        void Update(bool reset);
        void ClearSelection();

        Vector2 ToSelectionSpace(Vector2 v);
        Vector2 FromSelectionSpace(Vector2 v);
    }
}