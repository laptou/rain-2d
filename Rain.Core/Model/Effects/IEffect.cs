using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Imaging;

namespace Rain.Core.Model.Effects
{
    public interface IEffect : IDisposable
    {
        void SetInput(int index, IRenderImage bitmap);
        void SetInput(int index, IEffect effect);

        /// <summary>
        ///     Gets the native object representing the effect.
        /// </summary>
        /// <typeparam name="T">The type of object that is expected.</typeparam>
        /// <returns>The native object representing the effect.</returns>
        T Unwrap<T>() where T : class;
    }

    public interface IEffectLayer : IRenderContext
    {
        void ClearEffect();

        IEffect GetEffect();

        void PushEffect(IEffect effect);
    }
}