using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Paint;

namespace Rain.Renderer.Direct2D
{
    internal abstract class Brush : ResourceBase, IBrush
    {
        protected SharpDX.Direct2D1.Brush NativeBrush { get; set; }
        protected ReaderWriterLockSlim NativeBrushLock { get; } = new ReaderWriterLockSlim();

        public static bool operator ==(Brush brush, object o)
        {
            if (o == null &&
                brush?.NativeBrush == null) return true;

            return Equals(brush, o);
        }

        public static implicit operator SharpDX.Direct2D1.Brush(Brush brush)
        {
            if (brush == null) return null;

            brush.NativeBrushLock.TryEnterReadLock(-1);
            var native = brush.NativeBrush;
            brush.NativeBrushLock.ExitReadLock();

            return native;
        }

        public static bool operator !=(Brush brush, object o) { return !(brush == o); }

        #region IBrush Members

        public override void Dispose()
        {
            NativeBrush?.Dispose();
            NativeBrushLock?.Dispose();
            GC.SuppressFinalize(this);
            base.Dispose();
        }

        public T Unwrap<T>() where T : class { return NativeBrush as T; }

        public float Opacity
        {
            get => NativeBrush.Opacity;
            set
            {
                NativeBrush.Opacity = value;
                RaisePropertyChanged();
            }
        }

        public Matrix3x2 Transform
        {
            get => NativeBrush.Transform.Convert();
            set
            {
                NativeBrush.Transform = value.Convert();
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}