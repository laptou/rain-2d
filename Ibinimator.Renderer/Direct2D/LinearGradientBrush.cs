using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model.Paint;

using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

using GradientStop = Ibinimator.Core.Model.Paint.GradientStop;

namespace Ibinimator.Renderer.Direct2D
{
    internal class LinearGradientBrush : GradientBrush, ILinearGradientBrush
    {
        private SpreadMethod _spreadMethod;

        public LinearGradientBrush(
            RenderTarget target, IEnumerable<GradientStop> stops, RawVector2 start, RawVector2 end)
            : base(target, stops)
        {
            using (var nativeStops = ConvertStops())
            {
                NativeBrush = new SharpDX.Direct2D1.LinearGradientBrush(Target,
                                                                        new
                                                                            LinearGradientBrushProperties
                                                                            {
                                                                                StartPoint =
                                                                                    new RawVector2(
                                                                                        start.X,
                                                                                        start.Y),
                                                                                EndPoint =
                                                                                    new RawVector2(
                                                                                        end.X,
                                                                                        end.Y)
                                                                            },
                                                                        nativeStops);
            }
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
            object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            if (Stops.Count > 0) // avoid access violation exceptions when the list is cleared
                RecreateBrush();
        }

        private void RecreateBrush()
        {
            var old = (SharpDX.Direct2D1.LinearGradientBrush) NativeBrush;

            NativeBrushLock.EnterWriteLock();

            using (var nativeStops = ConvertStops())
            {
                NativeBrush = new SharpDX.Direct2D1.LinearGradientBrush(Target,
                                                                        new
                                                                            LinearGradientBrushProperties
                                                                            {
                                                                                StartPoint =
                                                                                    new RawVector2(
                                                                                        StartX,
                                                                                        StartY),
                                                                                EndPoint =
                                                                                    new RawVector2(
                                                                                        EndX,
                                                                                        EndY)
                                                                            },
                                                                        nativeStops);
            }

            old.Dispose();

            NativeBrushLock.ExitWriteLock();
        }

        #region ILinearGradientBrush Members

        public override void Dispose()
        {
            var old = (SharpDX.Direct2D1.LinearGradientBrush) NativeBrush;

            if (!old.IsDisposed)
            {
                // the gradient stop collection continues to exist until it is decoupled
                // and then manually disposed of
                var stops = old.GradientStopCollection;

                base.Dispose();

                stops.Dispose();
            }
            else
            {
                base.Dispose();
            }
        }

        public float EndX
        {
            get => ((SharpDX.Direct2D1.LinearGradientBrush) NativeBrush).EndPoint.X;
            set
            {
                ((SharpDX.Direct2D1.LinearGradientBrush) NativeBrush).EndPoint =
                    new RawVector2(value, EndY);
                RaisePropertyChanged();
            }
        }

        public float EndY
        {
            get => ((SharpDX.Direct2D1.LinearGradientBrush) NativeBrush).EndPoint.Y;
            set
            {
                ((SharpDX.Direct2D1.LinearGradientBrush) NativeBrush).EndPoint =
                    new RawVector2(EndX, value);
                RaisePropertyChanged();
            }
        }

        public float StartX
        {
            get => ((SharpDX.Direct2D1.LinearGradientBrush) NativeBrush).StartPoint.X;
            set
            {
                ((SharpDX.Direct2D1.LinearGradientBrush) NativeBrush).StartPoint =
                    new RawVector2(value, StartY);
                RaisePropertyChanged();
            }
        }

        public float StartY
        {
            get => ((SharpDX.Direct2D1.LinearGradientBrush) NativeBrush).StartPoint.Y;
            set
            {
                ((SharpDX.Direct2D1.LinearGradientBrush) NativeBrush).StartPoint =
                    new RawVector2(StartX, value);
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}