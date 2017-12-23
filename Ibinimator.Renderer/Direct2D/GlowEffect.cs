using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core;

namespace Ibinimator.Renderer.Direct2D {
    public class GlowEffect : Effect, IGlowEffect
    {
        private readonly SharpDX.Direct2D1.Effect blur;
        private readonly SharpDX.Direct2D1.Effect composite;

        public GlowEffect(SharpDX.Direct2D1.DeviceContext dc)
        {
            blur = new SharpDX.Direct2D1.Effect(dc, SharpDX.Direct2D1.Effect.GaussianBlur);

            composite = new SharpDX.Direct2D1.Effect(dc, SharpDX.Direct2D1.Effect.Composite);
            composite.SetInputEffect(0, blur, false);
        }

        public override SharpDX.Direct2D1.Image GetOutput() { return composite.Output; }

        #region IGlowEffect Members

        public override void Dispose()
        {
            blur.Dispose();
            composite.Dispose();
        }

        public override void SetInput(int index, IBitmap bitmap)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));

            blur.SetInput(0, bitmap.Unwrap<SharpDX.Direct2D1.Bitmap>(), true);
            composite.SetInput(1, bitmap.Unwrap<SharpDX.Direct2D1.Bitmap>(), true);
        }

        public override void SetInput(int index, IEffect effect)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));

            blur.SetInputEffect(0, effect.Unwrap<SharpDX.Direct2D1.Effect>());
            composite.SetInputEffect(1, effect.Unwrap<SharpDX.Direct2D1.Effect>());
        }

        public override T Unwrap<T>() { return composite as T; }

        public float Radius
        {
            get => blur.GetFloatValue((int) SharpDX.Direct2D1.GaussianBlurProperties.StandardDeviation);
            set => blur.SetValue((int) SharpDX.Direct2D1.GaussianBlurProperties.StandardDeviation,
                                 value);
        }

        #endregion
    }
}