﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Paint;
using Rain.Core.Utility;

namespace Rain.Renderer.WPF
{
    using WPF = System.Windows.Media;

    internal class Pen : ResourceBase, IPen
    {
        private Brush    _brush;
        private float    _dashOffset;
        private LineCap  _lineCap;
        private LineJoin _lineJoin;
        private float    _miterLimit;
        private float    _width;

        public Pen(Brush brush) : this(1, brush) { }

        public Pen(float width, Brush brush) : this(width, brush, Enumerable.Empty<float>()) { }

        public Pen(float width, Brush brush, IEnumerable<float> dashes) : this(
            width,
            brush,
            dashes,
            0,
            LineCap.Butt,
            LineJoin.Miter,
            4) { }

        public Pen(
            float width, Brush brush, IEnumerable<float> dashes, float dashOffset, LineCap lineCap, LineJoin lineJoin,
            float miterLimit)
        {
            Width = width;
            Brush = brush;

            Dashes = new ObservableList<float>(dashes);

            DashOffset = dashOffset;
            LineCap = lineCap;
            LineJoin = lineJoin;
            MiterLimit = miterLimit;
        }

        public Brush Brush
        {
            get => _brush;
            set
            {
                _brush = value;
                RaisePropertyChanged();
            }
        }

        public static implicit operator WPF.Pen(Pen pen)
        {
            if (pen == null) return null;

            return new WPF.Pen(pen.Brush, pen.Width)
            {
                DashCap = (WPF.PenLineCap) pen.LineCap,
                StartLineCap = (WPF.PenLineCap) pen.LineCap,
                EndLineCap = (WPF.PenLineCap) pen.LineCap,
                LineJoin = (WPF.PenLineJoin) pen.LineJoin,
                DashStyle = new WPF.DashStyle(pen.Dashes.Select(f => (double) f), pen.DashOffset),
                MiterLimit = pen.MiterLimit
            };
        }

        #region IPen Members

        public override void Dispose()
        {
            Brush?.Dispose();
            base.Dispose();
        }

        public IList<float> Dashes { get; }

        public float DashOffset
        {
            get => _dashOffset;
            set
            {
                _dashOffset = value;
                RaisePropertyChanged();
            }
        }

        public LineCap LineCap
        {
            get => _lineCap;
            set
            {
                _lineCap = value;
                RaisePropertyChanged();
            }
        }

        public LineJoin LineJoin
        {
            get => _lineJoin;
            set
            {
                _lineJoin = value;
                RaisePropertyChanged();
            }
        }

        public float MiterLimit
        {
            get => _miterLimit;
            set
            {
                _miterLimit = value;
                RaisePropertyChanged();
            }
        }

        public float Width
        {
            get => _width;
            set
            {
                _width = value;
                RaisePropertyChanged();
            }
        }

        IBrush IPen.Brush
        {
            get => Brush;
            set => Brush = value as Brush;
        }

        #endregion
    }
}