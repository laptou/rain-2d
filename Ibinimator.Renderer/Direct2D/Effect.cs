using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Core.Model.Effects;
using Ibinimator.Core.Model.Paint;

namespace Ibinimator.Renderer.Direct2D {
    public abstract class Effect : IEffect
    {
        public abstract SharpDX.Direct2D1.Image GetOutput();

        #region IEffect Members

        public abstract void Dispose();

        public abstract void SetInput(int index, IBitmap bitmap);
        public abstract void SetInput(int index, IEffect effect);
        public abstract T Unwrap<T>() where T : class;

        #endregion
    }
}