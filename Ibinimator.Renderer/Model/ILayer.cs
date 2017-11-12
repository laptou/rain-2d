using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Model
{
    public interface ILayer
    {
        Matrix3x2 AbsoluteTransform { get; }
        IGeometricLayer Clip { get; set; }
        string DefaultName { get; }
        float Height { get; set; }
        Guid Id { get; }
        ILayer Mask { get; set; }
        string Name { get; set; }
        float Opacity { get; set; }
        Group Parent { get; }
        bool Selected { get; set; }
        Matrix3x2 Transform { get; }
        float Width { get; set; }
        Matrix3x2 WorldTransform { get; }

        event EventHandler BoundsChanged;

        void ApplyTransform(Matrix3x2? local = null, Matrix3x2? global = null);
        Layer Find(Guid id);
        RectangleF GetBounds(ICacheManager cache);
        IDisposable GetResource(ICacheManager cache, int id);
        Layer Hit(ICacheManager cache, Vector2 point, bool includeMe);
        T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe) where T : Layer;
        void Render(RenderContext target, ICacheManager cache);
    }
}