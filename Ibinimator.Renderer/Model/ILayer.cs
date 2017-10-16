using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Matrix3x2 = System.Numerics.Matrix3x2;
using Vector2 = System.Numerics.Vector2;

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
        Vector2 Origin { get; set; }
        Group Parent { get; }
        Vector2 Position { get; set; }
        float Rotation { get; set; }
        Vector2 Scale { get; set; }
        bool Selected { get; set; }
        float Shear { get; set; }
        Matrix3x2 Transform { get; }
        float Width { get; set; }
        Matrix3x2 WorldTransform { get; }

        void ApplyTransform(Matrix3x2 transform);
        Layer Find(Guid id);
        RectangleF GetBounds(ICacheManager cache);
        IDisposable GetResource(ICacheManager cache, int id);
        Layer Hit(ICacheManager cache, Vector2 point, bool includeMe);
        T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe) where T : Layer;
        void Render(RenderContext target, ICacheManager cache);

        event EventHandler BoundsChanged;
    }
}