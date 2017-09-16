using System;
using System.Xml.Linq;
using Ibinimator.Service;
using SharpDX;
using SharpDX.Direct2D1;

namespace Ibinimator.Model
{
    public interface ILayer
    {
        Matrix3x2 AbsoluteTransform { get; }
        string DefaultName { get; }
        float Height { get; set; }
        Guid Id { get; }
        ILayer Mask { get; set; }
        IGeometricLayer Clip { get; set; }
        string Name { get; set; }
        float Opacity { get; set; }
        Group Parent { get; }
        Vector2 Position { get; set; }
        float Rotation { get; set; }
        Vector2 Scale { get; set; }
        bool Selected { get; set; }
        float Shear { get; set; }
        Matrix3x2 Transform { get; }
        float Width { get; set; }
        Matrix3x2 WorldTransform { get; }

        RectangleF GetBounds(ICacheManager cache);
        XElement GetElement();
        Layer Hit(ICacheManager cache, Vector2 point, bool includeMe);
        T Hit<T>(ICacheManager cache, Vector2 point, bool includeMe) where T : Layer;
        void Render(RenderTarget target, ICacheManager cache);
        IDisposable GetResource(ICacheManager cache, int id);
        Layer Find(Guid id);
    }
}