using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using Rain.Core.Model.Paint;

using GradientStop = Rain.Core.Model.Paint.GradientStop;

namespace Rain.Renderer.WPF
{
    internal class LinearGradientBrush : GradientBrush, ILinearGradientBrush
    {
        public LinearGradientBrush(IEnumerable<GradientStop> stops, Point start, Point end) : base(stops)
        {
            WpfBrush = new System.Windows.Media.LinearGradientBrush(ConvertStops(), start, end)
            {
                MappingMode = Space == GradientSpace.Absolute
                                  ? BrushMappingMode.Absolute
                                  : BrushMappingMode.RelativeToBoundingBox
            };
        }

        public override SpreadMethod SpreadMethod
        {
            get =>
                (SpreadMethod) ((System.Windows.Media.LinearGradientBrush) WpfBrush).SpreadMethod;
            set => ((System.Windows.Media.LinearGradientBrush) WpfBrush).SpreadMethod = (GradientSpreadMethod) value;
        }

        protected override void OnStopsChanged(
            object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            ((System.Windows.Media.LinearGradientBrush) WpfBrush).GradientStops = ConvertStops();
        }

        #region ILinearGradientBrush Members

        public float EndX
        {
            get => (float) ((System.Windows.Media.LinearGradientBrush) WpfBrush).EndPoint.X;
            set
            {
                ((System.Windows.Media.LinearGradientBrush) WpfBrush).EndPoint = new Point(value, EndY);
                RaisePropertyChanged();
            }
        }

        public float EndY
        {
            get => (float) ((System.Windows.Media.LinearGradientBrush) WpfBrush).EndPoint.Y;
            set
            {
                ((System.Windows.Media.LinearGradientBrush) WpfBrush).EndPoint = new Point(EndX, value);
                RaisePropertyChanged();
            }
        }

        public float StartX
        {
            get => (float) ((System.Windows.Media.LinearGradientBrush) WpfBrush).StartPoint.X;
            set
            {
                ((System.Windows.Media.LinearGradientBrush) WpfBrush).StartPoint = new Point(value, StartY);
                RaisePropertyChanged();
            }
        }

        public float StartY
        {
            get => (float) ((System.Windows.Media.LinearGradientBrush) WpfBrush).StartPoint.Y;
            set
            {
                ((System.Windows.Media.LinearGradientBrush) WpfBrush).StartPoint = new Point(StartX, value);
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}