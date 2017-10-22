using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;
using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;

namespace Ibinimator.Renderer.Direct2D
{
    internal abstract class GradientBrush : Brush, IGradientBrush
    {
        protected readonly RenderTarget Target;

        protected GradientBrush(RenderTarget target, IEnumerable<GradientStop> stops)
        {
            Target = target;
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
            return new GradientStopCollection(Target,
                                              Stops.Select(s => new SharpDX.Direct2D1.GradientStop
                                                   {
                                                       Color = new RawColor4(
                                                           s.Color.R,
                                                           s.Color.G,
                                                           s.Color.B,
                                                           s.Color.A),
                                                       Position = s.Offset
                                                   })
                                                   .ToArray(),
                                              (ExtendMode) SpreadMethod);
        }

        #region IGradientBrush Members

        public IList<GradientStop> Stops { get; }

        #endregion
    }
}