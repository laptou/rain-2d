using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Model;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace Ibinimator.Renderer.Direct2D
{
    internal class LinearGradientBrush : GradientBrush, ILinearGradientBrush
    {
        private SpreadMethod _spreadMethod;

        public LinearGradientBrush(
            RenderTarget target,
            IEnumerable<GradientStop> stops,
            RawVector2 start,
            RawVector2 end) : base(target, stops)
        {
            Direct2DBrush = new SharpDX.Direct2D1.LinearGradientBrush(target,
                                                                      new LinearGradientBrushProperties
                                                                      {
                                                                          StartPoint = start,
                                                                          EndPoint = end
                                                                      },
                                                                      ConvertStops());
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

        protected override void OnStopsChanged(
            object sender,
            NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            RecreateBrush();
        }

        private void RecreateBrush()
        {
            Direct2DBrush = new SharpDX.Direct2D1.LinearGradientBrush(Target,
                                                                      new LinearGradientBrushProperties
                                                                      {
                                                                          StartPoint = new RawVector2(StartX, StartY),
                                                                          EndPoint = new RawVector2(EndX, EndY)
                                                                      },
                                                                      ConvertStops());
        }

        #region ILinearGradientBrush Members

        public float EndX
        {
            get => ((SharpDX.Direct2D1.LinearGradientBrush) Direct2DBrush).EndPoint.X;
            set
            {
                ((SharpDX.Direct2D1.LinearGradientBrush) Direct2DBrush).EndPoint = new RawVector2(value, EndY);
                RaisePropertyChanged();
            }
        }

        public float EndY
        {
            get => ((SharpDX.Direct2D1.LinearGradientBrush) Direct2DBrush).EndPoint.Y;
            set
            {
                ((SharpDX.Direct2D1.LinearGradientBrush) Direct2DBrush).EndPoint = new RawVector2(EndX, value);
                RaisePropertyChanged();
            }
        }

        public float StartX
        {
            get => ((SharpDX.Direct2D1.LinearGradientBrush) Direct2DBrush).StartPoint.X;
            set
            {
                ((SharpDX.Direct2D1.LinearGradientBrush) Direct2DBrush).StartPoint = new RawVector2(value, StartY);
                RaisePropertyChanged();
            }
        }

        public float StartY
        {
            get => ((SharpDX.Direct2D1.LinearGradientBrush) Direct2DBrush).StartPoint.Y;
            set
            {
                ((SharpDX.Direct2D1.LinearGradientBrush) Direct2DBrush).StartPoint = new RawVector2(StartX, value);
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}