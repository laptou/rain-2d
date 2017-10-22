﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;
using Color = System.Windows.Media.Color;

namespace Ibinimator.Renderer.WPF
{
    internal abstract class GradientBrush : Brush, IGradientBrush
    {
        protected GradientBrush(IEnumerable<GradientStop> stops)
        {
            var list = new ObservableList<GradientStop>(stops);
            list.CollectionChanged += OnStopsChanged;
            Stops = list;
        }

        public abstract SpreadMethod SpreadMethod { get; set; }

        protected abstract void OnStopsChanged(
            object sender,
            NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs);

        protected GradientStopCollection ConvertStops()
        {
            return new GradientStopCollection(Stops.Select(s => new System.Windows.Media.GradientStop
            {
                Color = Color.FromArgb(
                    (byte) (s.Color.R * 255),
                    (byte) (s.Color.G * 255),
                    (byte) (s.Color.B * 255),
                    (byte) (s.Color.A * 255))
            }));
        }

        #region IGradientBrush Members

        public IList<GradientStop> Stops { get; }

        #endregion
    }
}