using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Ibinimator.Renderer.WPF
{
    internal class Brush : ResourceBase, IBrush
    {
        protected System.Windows.Media.Brush WpfBrush { get; set; }

        public static implicit operator System.Windows.Media.Brush(Brush brush) { return brush?.WpfBrush; }

        #region IBrush Members

        public override void Dispose()
        {
            WpfBrush = null;

            base.Dispose();
        }

        public override void Optimize() { WpfBrush.Freeze(); }

        public float Opacity
        {
            get => (float) WpfBrush.Opacity;
            set
            {
                WpfBrush.Opacity = value;
                RaisePropertyChanged();
            }
        }

        public Matrix3x2 Transform
        {
            get => WpfBrush.Transform.Value.Convert();
            set
            {
                WpfBrush.Transform = new MatrixTransform(value.Convert());
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}