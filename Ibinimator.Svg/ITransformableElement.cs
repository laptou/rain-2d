using System.Numerics;

namespace Ibinimator.Svg
{
    public interface ITransformableElement
    {
        Matrix3x2 Transform { get; set; }
    }
}