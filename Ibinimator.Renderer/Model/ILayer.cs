using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public interface ILayer : INotifyPropertyChanged, INotifyPropertyChanging
    {
        string DefaultName { get; }
        string Name { get; set; }
        Guid Id { get; }

        Matrix3x2 AbsoluteTransform { get; }
        Matrix3x2 WorldTransform { get; }
        Matrix3x2 Transform { get; }

        float Height { get; set; }
        float Width { get; set; }

        IEnumerable<ILayer> Flatten();
        ILayer Find(Guid id);
        IContainerLayer Parent { get; set; }
        IGeometricLayer Clip { get; set; }

        ILayer Mask { get; set; }
        float Opacity { get; set; }
        bool Selected { get; set; }

        event EventHandler BoundsChanged;

        void ApplyTransform(Matrix3x2? local = null, Matrix3x2? global = null);
        RectangleF GetBounds(ICacheManager cache);

        ILayer Hit(ICacheManager cache, Vector2 point, bool includeMe);
        T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe) where T : ILayer;

        void Render(RenderContext target, ICacheManager cache, IViewManager view);
    }
}