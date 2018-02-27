using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model.Effects;
using Rain.Core.Model.Imaging;

using SharpDX.Direct2D1;

namespace Rain.Renderer.Direct2D {
    public class ScaleEffect : Effect, IScaleEffect
    {
        private readonly SharpDX.Direct2D1.Effect _effect;

        public ScaleEffect(DeviceContext dc)
        {
            _effect = new SharpDX.Direct2D1.Effect(dc, SharpDX.Direct2D1.Effect.Scale);
        }

        public override Image GetOutput() { return _effect.Output; }

        #region IScaleEffect Members

        public override void Dispose() { _effect?.Dispose(); }

        public override void SetInput(int index, IRenderImage bitmap)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));

            _effect.SetInput(0, bitmap.Unwrap<SharpDX.Direct2D1.Bitmap>(), true);
        }

        public override void SetInput(int index, IEffect effect)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));

            _effect.SetInputEffect(0, effect.Unwrap<SharpDX.Direct2D1.Effect>());
        }

        public override T Unwrap<T>() { return _effect as T; }

        /// <inheritdoc />
        public Vector2 Factor
        {
            get => _effect.GetVector2Value((int) ScaleProperties.Scale).Convert();
            set => _effect.SetValue((int) ScaleProperties.Scale, value.ConvertRaw());
        }

        /// <inheritdoc />
        public Vector2 Origin
        {
            get => _effect.GetVector2Value((int) ScaleProperties.CenterPoint).Convert();
            set => _effect.SetValue((int) ScaleProperties.CenterPoint, value.ConvertRaw());
        }

        /// <inheritdoc />
        public ScaleMode ScaleMode
        {
            get => (ScaleMode) _effect.GetEnumValue<ScaleInterpolationMode>(
                (int) ScaleProperties.InterpolationMode);
            set =>
                _effect.SetEnumValue((int) ScaleProperties.InterpolationMode, (ScaleInterpolationMode) value);
        }

        /// <inheritdoc />
        public bool SoftBorder
        {
            get => _effect.GetEnumValue<BorderMode>((int) ScaleProperties.BorderMode) ==
                   BorderMode.Soft;
            set => _effect.SetEnumValue((int) ScaleProperties.BorderMode,
                                        value ? BorderMode.Soft : BorderMode.Hard);
        }

        #endregion
    }
}