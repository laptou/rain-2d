using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace Ibinimator.Renderer.Direct2D
{
    internal class RadialGradientBrush : GradientBrush, IRadialGradientBrush
    {
        private SpreadMethod _spreadMethod;

        public RadialGradientBrush(RenderTarget target, IEnumerable<GradientStop> stops,
            RawVector2 center, RawVector2 radii, RawVector2 focus) : base(target, stops)
        {
            Direct2DBrush = new SharpDX.Direct2D1.RadialGradientBrush(target, new RadialGradientBrushProperties
            {
                Center = center,
                GradientOriginOffset = new RawVector2(
                    focus.X - center.X,
                    focus.Y - center.Y),
                RadiusX = radii.X,
                RadiusY = radii.Y
            }, ConvertStops());
        }

        public override SpreadMethod SpreadMethod
        {
            get => _spreadMethod;
            set
            {
                _spreadMethod = value;
                RecreateBrush();
                RaisePropertyChanged();
            }
        }

        protected override void OnStopsChanged(object sender,
            NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            RecreateBrush();
        }

        private void RecreateBrush()
        {
            Direct2DBrush = new SharpDX.Direct2D1.RadialGradientBrush(Target, new RadialGradientBrushProperties
            {
                Center = new RawVector2(CenterX, CenterY),
                GradientOriginOffset = new RawVector2(
                    FocusX - CenterX,
                    FocusY - CenterY),
                RadiusX = RadiusX,
                RadiusY = RadiusY
            }, ConvertStops());
        }

        #region IRadialGradientBrush Members

        public float CenterX
        {
            get => ((SharpDX.Direct2D1.RadialGradientBrush) Direct2DBrush).Center.X;
            set
            {
                ((SharpDX.Direct2D1.RadialGradientBrush) Direct2DBrush).Center = new RawVector2(value, CenterY);
                RaisePropertyChanged();
            }
        }

        public float CenterY
        {
            get => ((SharpDX.Direct2D1.RadialGradientBrush) Direct2DBrush).Center.Y;
            set
            {
                ((SharpDX.Direct2D1.RadialGradientBrush) Direct2DBrush).Center = new RawVector2(CenterX, value);
                RaisePropertyChanged();
            }
        }

        public float FocusX
        {
            get => ((SharpDX.Direct2D1.RadialGradientBrush) Direct2DBrush).GradientOriginOffset.X + CenterX;
            set
            {
                ((SharpDX.Direct2D1.RadialGradientBrush) Direct2DBrush).GradientOriginOffset =
                    new RawVector2(value - CenterX, FocusY);
                RaisePropertyChanged();
            }
        }

        public float FocusY
        {
            get => ((SharpDX.Direct2D1.RadialGradientBrush) Direct2DBrush).GradientOriginOffset.Y + CenterY;
            set
            {
                ((SharpDX.Direct2D1.RadialGradientBrush) Direct2DBrush).GradientOriginOffset =
                    new RawVector2(FocusX, value - CenterY);
                RaisePropertyChanged();
            }
        }

        public float RadiusX
        {
            get => ((SharpDX.Direct2D1.RadialGradientBrush) Direct2DBrush).RadiusX;
            set
            {
                ((SharpDX.Direct2D1.RadialGradientBrush) Direct2DBrush).RadiusX = value;
                RaisePropertyChanged();
            }
        }

        public float RadiusY
        {
            get => ((SharpDX.Direct2D1.RadialGradientBrush) Direct2DBrush).RadiusY;
            set
            {
                ((SharpDX.Direct2D1.RadialGradientBrush) Direct2DBrush).RadiusY = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}