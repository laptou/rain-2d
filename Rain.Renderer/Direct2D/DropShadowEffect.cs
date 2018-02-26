using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Effects;
using Rain.Core.Model.Imaging;
using Rain.Core.Model.Paint;

using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace Rain.Renderer.Direct2D
{
    public class DropShadowEffect : Effect, IDropShadowEffect
    {
        private readonly SharpDX.Direct2D1.Effect composite;
        private readonly SharpDX.Direct2D1.Effect shadow;

        public DropShadowEffect(DeviceContext dc)
        {
            shadow = new SharpDX.Direct2D1.Effect(dc, SharpDX.Direct2D1.Effect.Shadow);

            composite = new SharpDX.Direct2D1.Effect(dc, SharpDX.Direct2D1.Effect.Composite);
            composite.SetInputEffect(0, shadow, false);
        }

        public override SharpDX.Direct2D1.Image GetOutput() { return composite.Output; }

        #region IDropShadowEffect Members

        public override void Dispose()
        {
            shadow.Dispose();
            composite.Dispose();
        }

        public override void SetInput(int index, IRenderImage bitmap)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));

            shadow.SetInput(0, bitmap.Unwrap<SharpDX.Direct2D1.Bitmap>(), true);
            composite.SetInput(1, bitmap.Unwrap<SharpDX.Direct2D1.Bitmap>(), true);
        }

        public override void SetInput(int index, IEffect effect)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));

            shadow.SetInputEffect(0, effect.Unwrap<SharpDX.Direct2D1.Effect>());
            composite.SetInputEffect(1, effect.Unwrap<SharpDX.Direct2D1.Effect>());
        }

        public override T Unwrap<T>() { return shadow as T; }

        public Color Color
        {
            get => shadow.GetColor4Value((int) ShadowProperties.Color).Convert();
            set => shadow.SetValue((int) ShadowProperties.Color, (RawColor4) value.Convert());
        }

        public float Radius
        {
            get => shadow.GetFloatValue((int) ShadowProperties.BlurStandardDeviation);
            set => shadow.SetValue((int) ShadowProperties.BlurStandardDeviation, value);
        }

        #endregion
    }
}