using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

using Rain.Core.Model.Paint;
using Rain.Core.Utility;

using GradientStop = Rain.Core.Model.Paint.GradientStop;

namespace Ibinimator.Renderer.WPF
{
    internal abstract class GradientBrush : Brush, IGradientBrush
    {
        private GradientSpace _space;

        protected GradientBrush(IEnumerable<GradientStop> stops)
        {
            var list = new ObservableList<GradientStop>(stops);
            list.CollectionChanged += OnStopsChanged;
            Stops = list;
        }

        public abstract SpreadMethod SpreadMethod { get; set; }

        protected abstract void OnStopsChanged(
            object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs);

        protected GradientStopCollection ConvertStops()
        {
            return new GradientStopCollection(Stops.Select(
                                                  s => new System.Windows.Media.GradientStop
                                                  {
                                                      Color = Color.FromArgb(
                                                          (byte) (s.Color.Alpha * 255),
                                                          (byte) (s.Color.Red * 255),
                                                          (byte) (s.Color.Green * 255),
                                                          (byte) (s.Color.Blue * 255)),
                                                      Offset = s.Offset
                                                  }));
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