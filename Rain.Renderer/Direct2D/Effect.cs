using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Effects;
using Rain.Core.Model.Imaging;

using SharpDX.Direct2D1;

namespace Rain.Renderer.Direct2D
{
    public abstract class Effect : IEffect
    {
        public abstract Image GetOutput();

        #region IEffect Members

        public abstract void Dispose();

        public abstract void SetInput(int index, IRenderImage bitmap);
        public abstract void SetInput(int index, IEffect effect);
        public abstract T Unwrap<T>() where T : class;

        #endregion
    }
}