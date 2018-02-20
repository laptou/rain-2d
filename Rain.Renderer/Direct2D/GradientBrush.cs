using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Paint;
using Rain.Core.Utility;

using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

using GradientStop = Rain.Core.Model.Paint.GradientStop;

namespace Ibinimator.Renderer.Direct2D
{
    internal abstract class GradientBrush : Brush, IGradientBrush
    {
        protected readonly RenderTarget  Target;
        private            GradientSpace _space;

        protected GradientBrush(RenderTarget target, IEnumerable<GradientStop> stops)
        {
            Target = target;
            var list = new ObservableList<GradientStop>(stops);
            list.CollectionChanged += OnStopsChanged;
            Stops = list;
        }

        public abstract SpreadMethod SpreadMethod { get; set; }

        protected abstract void OnStopsChanged(
            object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs);

        protected GradientStopCollection ConvertStops()
        {
            return new GradientStopCollection(Target,
                                              Stops.Select(s => new SharpDX.Direct2D1.GradientStop
                                                    {
                                                        Color = new RawColor4(
                                                            s.Color.Red,
                                                            s.Color.Green,
                                                            s.Color.Blue,
                                                            s.Color.Alpha),
                                                        Position = s.Offset
                                                    })
                                                   .ToArray(),
                                              (ExtendMode) SpreadMethod);
        }

        #region IGradientBrush Members

        public GradientSpace Space
        {
            get => _space;
            set
            {
                _space = value;
                RaisePropertyChanged();
            }
        }

        public ObservableList<GradientStop> Stops { get; }

        #endregion
    }
}