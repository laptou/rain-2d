using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg
{
    public class Animatable<T>
    {
        public Animatable(T baseValue, IInterpolator<T> interpolator)
        {
            BaseValue = baseValue;
            Interpolator = interpolator;
        }

        public IInterpolator<T> Interpolator { get; set; }

        public T BaseValue { get; set; }

        public T GetValue(float time) => Interpolator.ProvideValue(time);

        public static implicit operator T(Animatable<T> animatable)
        {
            return animatable.BaseValue;
        }
    }
}