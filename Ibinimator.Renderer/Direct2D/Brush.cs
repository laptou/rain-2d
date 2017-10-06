using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer.Direct2D
{
    internal abstract class Brush : PropertyChangedBase, IBrush
    {
        protected SharpDX.Direct2D1.Brush Direct2DBrush { get; set; }

        public static implicit operator SharpDX.Direct2D1.Brush(Brush brush)
        {
            return brush.Direct2DBrush;
        }

        #region IBrush Members

        public void Dispose()
        {
            Direct2DBrush?.Dispose();
        }

        public void Optimize()
        {
            // this doesn't do anything; Direct2D objects aren't freezable
        }

        public float Opacity
        {
            get => Direct2DBrush.Opacity;
            set
            {
                Direct2DBrush.Opacity = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}