﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Media;

using Rain.Core.Model;
using Rain.Core.Model.Paint;

namespace Rain.Renderer.WPF
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

        public T Unwrap<T>() where T : class { return WpfBrush as T; }

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