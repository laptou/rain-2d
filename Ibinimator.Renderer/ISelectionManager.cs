using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Renderer.Model;

namespace Ibinimator.Renderer
{
    public interface ISelectionManager : IArtContextManager
    {
        IBitmap Cursor { get; set; }
        IList<Layer> Selection { get; }
        RectangleF SelectionBounds { get; }
        Matrix3x2 SelectionTransform { get; }

        event EventHandler Updated;
        void ClearSelection();
        
        void MouseDown(Vector2 pos);
        void MouseMove(Vector2 pos);
        void MouseUp(Vector2 pos);
        
        void KeyDown(Key key, ModifierKeys modifiers);
        void KeyUp(Key key, ModifierKeys modifiers);

        void Render(RenderContext target, ICacheManager cache);

        Vector2 FromSelectionSpace(Vector2 v);
        Vector2 ToSelectionSpace(Vector2 v);

        void Transform(Vector2 scale, Vector2 translate, float rotate, float shear, Vector2 origin);
        void Update(bool reset);
    }
}