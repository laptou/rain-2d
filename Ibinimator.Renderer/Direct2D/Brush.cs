using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Model;

namespace Ibinimator.Renderer.Direct2D
{
    internal abstract class Brush : ResourceBase, IBrush
    {
        protected SharpDX.Direct2D1.Brush Direct2DBrush { get; set; }

        public static bool operator ==(Brush brush, object o)
        {
            if (o == null && brush?.Direct2DBrush == null) return true;
            return Equals(brush, o);
        }

        public static implicit operator SharpDX.Direct2D1.Brush(Brush brush) { return brush?.Direct2DBrush; }

        public static bool operator !=(Brush brush, object o) { return !(brush == o); }

        #region IBrush Members

        public override void Dispose()
        {
            Direct2DBrush?.Dispose();

            base.Dispose();
        }

        public override void Optimize()
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

        public Matrix3x2 Transform
        {
            get => Direct2DBrush.Transform.Convert();
            set
            {
                Direct2DBrush.Transform = value.Convert();
                RaisePropertyChanged();
            }
        }

        public T Unwrap<T>() where T : class { return Direct2DBrush as T; }

        #endregion
    }
}