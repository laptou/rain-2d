using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpDX.Mathematics.Interop;
using D2D1 = SharpDX.Direct2D1;

namespace Ibinimator.Renderer.Direct2D
{
    internal sealed class SolidColorBrush : Brush, ISolidColorBrush
    {
        public SolidColorBrush(D2D1.RenderTarget target, Color color)
        {
            Direct2DBrush =
                new D2D1.SolidColorBrush(target,
                    new RawColor4(color.R, color.G, color.B, color.A));
        }

        #region ISolidColorBrush Members

        public Color Color
        {
            get => new Color(
                ((D2D1.SolidColorBrush) Direct2DBrush).Color.R,
                ((D2D1.SolidColorBrush) Direct2DBrush).Color.G,
                ((D2D1.SolidColorBrush) Direct2DBrush).Color.B,
                ((D2D1.SolidColorBrush) Direct2DBrush).Color.A);

            set => ((D2D1.SolidColorBrush) Direct2DBrush).Color =
                new RawColor4(
                    value.R,
                    value.G,
                    value.B,
                    value.A);
        }

        #endregion
    }
}