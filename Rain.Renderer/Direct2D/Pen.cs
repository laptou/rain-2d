﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model;
using Rain.Core.Model.Paint;
using Rain.Core.Utility;

using SharpDX.Direct2D1;

using LineJoin = Rain.Core.Model.LineJoin;

namespace Rain.Renderer.Direct2D
{
    internal class Pen : ResourceBase, IPen
    {
        private readonly RenderTarget _target;
        private          Brush        _brush;
        private          float        _dashOffset;
        private          LineCap      _lineCap;
        private          LineJoin     _lineJoin;
        private          float        _miterLimit;

        private float _width;

        public Pen(Brush brush, RenderTarget target) : this(1, brush, target) { }

        public Pen(float width, Brush brush, RenderTarget target) : this(
            width,
            brush,
            Enumerable.Empty<float>(),
            target) { }

        public Pen(float width, Brush brush, IEnumerable<float> dashes, RenderTarget target) : this(
            width,
            brush,
            dashes,
            0,
            LineCap.Butt,
            LineJoin.Miter,
            4,
            target) { }

        public Pen(
            float width, Brush brush, IEnumerable<float> dashes, float dashOffset, LineCap lineCap, LineJoin lineJoin,
            float miterLimit, RenderTarget target)
        {
            Width = width;
            Brush = brush;

            var list = new ObservableList<float>(dashes);
            list.CollectionChanged += (s, e) => { RecreateStyle(); };
            Dashes = list;

            DashOffset = dashOffset;
            LineCap = lineCap;
            LineJoin = lineJoin;
            MiterLimit = miterLimit;
            _target = target;

            RecreateStyle();
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

        public StrokeStyle1 Style { get; private set; }

        private void RecreateStyle()
        {
            var factory = _target.Factory.QueryInterface<Factory1>();
            var props = new StrokeStyleProperties1
            {
                TransformType = StrokeTransformType.Fixed,
                DashCap = (CapStyle) LineCap,
                StartCap = (CapStyle) LineCap,
                EndCap = (CapStyle) LineCap,
                LineJoin = (SharpDX.Direct2D1.LineJoin) LineJoin,
                DashStyle = Dashes.Count == 0 ? DashStyle.Solid : DashStyle.Custom,
                DashOffset = DashOffset,
                MiterLimit = MiterLimit
            };

            if (Dashes.Count == 0)
                Style = new StrokeStyle1(factory, props);
            else
                Style = new StrokeStyle1(factory, props, Dashes.ToArray());
        }

        #region IPen Members

        public override void Dispose()
        {
            Style.Dispose();
            base.Dispose();
        }

        /// <inheritdoc />
        public override bool Optimized => false;

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