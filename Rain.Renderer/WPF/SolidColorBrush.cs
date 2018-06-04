using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Paint;

namespace Rain.Renderer.WPF
{
    internal class SolidColorBrush : Brush, ISolidColorBrush
    {
        public SolidColorBrush(Color color) { WpfBrush = new System.Windows.Media.SolidColorBrush(color.Convert()); }

        #region ISolidColorBrush Members

        public Color Color
        {
            get => ((System.Windows.Media.SolidColorBrush) WpfBrush).Color.Convert();

            set => ((System.Windows.Media.SolidColorBrush) WpfBrush).Color = value.Convert();
        }

        #endregion
    }
}