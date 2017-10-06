using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.WPF
{
    internal class SolidColorBrush : Brush, ISolidColorBrush
    {
        public SolidColorBrush(Color color)
        {
            Color = color;
        }

        #region ISolidColorBrush Members

        public Color Color
        {
            get => new Color(
                ((System.Windows.Media.SolidColorBrush) WpfBrush).Color.R / 255f,
                ((System.Windows.Media.SolidColorBrush) WpfBrush).Color.G / 255f,
                ((System.Windows.Media.SolidColorBrush) WpfBrush).Color.B / 255f,
                ((System.Windows.Media.SolidColorBrush) WpfBrush).Color.A / 255f);

            set => ((System.Windows.Media.SolidColorBrush) WpfBrush).Color =
                System.Windows.Media.Color.FromArgb(
                    (byte) (value.R * 255),
                    (byte) (value.G * 255),
                    (byte) (value.B * 255),
                    (byte) (value.A * 255));
        }

        #endregion
    }

    #region Nested type: GradientBrush

    #endregion

    #region Nested type: LinearGradientBrush

    #endregion

    #region Nested type: RadialGradientBrush

    #endregion
}