using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;

using Color = System.Windows.Media.Color;
using GradientStop = Ibinimator.Core.GradientStop;

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
            object                           sender,
            NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs);

        protected GradientStopCollection ConvertStops()
        {
            return new GradientStopCollection(Stops.Select(s => new System.Windows.Media.GradientStop
            {
                Color = Color.FromArgb(
                    (byte) (s.Color.A * 255),
                    (byte) (s.Color.R * 255),
                    (byte) (s.Color.G * 255),
                    (byte) (s.Color.B * 255)),
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