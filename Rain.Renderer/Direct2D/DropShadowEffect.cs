using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Effects;
using Rain.Core.Model.Imaging;

using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace Rain.Renderer.Direct2D
{
    public class DropShadowEffect : Effect, IDropShadowEffect
    {
        private readonly SharpDX.Direct2D1.Effect _composite;
        private readonly SharpDX.Direct2D1.Effect _shadow;

        public DropShadowEffect(DeviceContext dc)
        {
            _shadow = new SharpDX.Direct2D1.Effect(dc, SharpDX.Direct2D1.Effect.Shadow);

            _composite = new SharpDX.Direct2D1.Effect(dc, SharpDX.Direct2D1.Effect.Composite);
            _composite.SetInputEffect(0, _shadow, false);
        }

        public override Image GetOutput() { return _composite.Output; }

        #region IDropShadowEffect Members

        public override void Dispose()
        {
            _shadow.Dispose();
            _composite.Dispose();
        }

        public override void SetInput(int index, IRenderImage bitmap)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));

            _shadow.SetInput(0, bitmap.Unwrap<SharpDX.Direct2D1.Bitmap>(), true);
            _composite.SetInput(1, bitmap.Unwrap<SharpDX.Direct2D1.Bitmap>(), true);
        }

        public override void SetInput(int index, IEffect effect)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));

            _shadow.SetInputEffect(0, effect.Unwrap<SharpDX.Direct2D1.Effect>());
            _composite.SetInputEffect(1, effect.Unwrap<SharpDX.Direct2D1.Effect>());
        }

        public override T Unwrap<T>() { return _composite as T; }

        public Color Color
        {
            get => _shadow.GetColor4Value((int) ShadowProperties.Color).Convert();
            set => _shadow.SetValue((int) ShadowProperties.Color, (RawColor4) value.Convert());
        }

        public float Radius
        {
            get => _shadow.GetFloatValue((int) ShadowProperties.BlurStandardDeviation);
            set => _shadow.SetValue((int) ShadowProperties.BlurStandardDeviation, value);
        }

        #endregion
    }
}