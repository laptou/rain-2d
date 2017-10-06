using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace Ibinimator.Renderer.Direct2D
{
    internal sealed class SolidColorBrush : Brush, ISolidColorBrush
    {
        public SolidColorBrush(RenderTarget target, Color color)
        {
            Direct2DBrush =
                new SharpDX.Direct2D1.SolidColorBrush(target,
                    new RawColor4(color.R, color.G, color.B, color.A));
        }

        #region ISolidColorBrush Members

        public Color Color
        {
            get => new Color(
                ((SharpDX.Direct2D1.SolidColorBrush) Direct2DBrush).Color.R,
                ((SharpDX.Direct2D1.SolidColorBrush) Direct2DBrush).Color.G,
                ((SharpDX.Direct2D1.SolidColorBrush) Direct2DBrush).Color.B,
                ((SharpDX.Direct2D1.SolidColorBrush) Direct2DBrush).Color.A);

            set => ((SharpDX.Direct2D1.SolidColorBrush) Direct2DBrush).Color =
                new RawColor4(
                    value.R,
                    value.G,
                    value.B,
                    value.A);
        }

        #endregion
    }
}