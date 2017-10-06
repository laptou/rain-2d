using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.WPF
{
    internal class Brush : PropertyChangedBase, IBrush
    {
        protected System.Windows.Media.Brush WpfBrush { get; set; }

        public static implicit operator System.Windows.Media.Brush(Brush brush)
        {
            return brush.WpfBrush;
        }

        #region IBrush Members

        public void Dispose()
        {
            WpfBrush = null;
        }

        public void Optimize()
        {
            WpfBrush.Freeze();
        }

        public float Opacity
        {
            get => (float) WpfBrush.Opacity;
            set => WpfBrush.Opacity = value;
        }

        #endregion
    }
}