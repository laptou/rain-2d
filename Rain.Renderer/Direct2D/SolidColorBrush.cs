using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Paint;

using SharpDX.Mathematics.Interop;

using D2D1 = SharpDX.Direct2D1;

namespace Rain.Renderer.Direct2D
{
    internal sealed class SolidColorBrush : Brush, ISolidColorBrush
    {
        public SolidColorBrush(D2D1.RenderTarget target, Color color)
        {
            NativeBrush = new D2D1.SolidColorBrush(target,
                                                   new RawColor4(
                                                       color.Red,
                                                       color.Green,
                                                       color.Blue,
                                                       color.Alpha));
        }

        #region ISolidColorBrush Members

        public Color Color
        {
            get => new Color(((D2D1.SolidColorBrush) NativeBrush).Color.R,
                             ((D2D1.SolidColorBrush) NativeBrush).Color.G,
                             ((D2D1.SolidColorBrush) NativeBrush).Color.B,
                             ((D2D1.SolidColorBrush) NativeBrush).Color.A);

            set => ((D2D1.SolidColorBrush) NativeBrush).Color =
                   new RawColor4(value.Red, value.Green, value.Blue, value.Alpha);
        }

        #endregion
    }
}