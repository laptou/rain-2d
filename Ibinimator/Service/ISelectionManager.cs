using System.Collections.Generic;
using Ibinimator.Model;
using SharpDX;
using SharpDX.Direct2D1;
using System.ComponentModel;
using System;
using Ibinimator.View.Control;

namespace Ibinimator.Service
{
    public interface ISelectionManager : INotifyPropertyChanged
    {
        ArtView ArtView { get; }
        Bitmap Cursor { get; set; }
        Model.Layer Root { get; }
        IList<Model.Layer> Selection { get; set; }
        RectangleF SelectionBounds { get; }
        float SelectionRotation { get; }
        float SelectionShear { get; }
        event EventHandler Updated;

        void Transform(Vector2 scale, Vector2 translate, float rotate, float shear, Vector2 origin);
        void OnMouseDown(Vector2 pos);
        void OnMouseMove(Vector2 pos);
        void OnMouseUp(Vector2 pos);
        void Render(RenderTarget target, ICacheManager cache);
        void Update(bool reset);
        void ClearSelection();
    }
}