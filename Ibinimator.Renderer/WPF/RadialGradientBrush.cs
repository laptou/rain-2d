using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using GradientStop = Ibinimator.Core.GradientStop;

namespace Ibinimator.Renderer.WPF
{
    internal class RadialGradientBrush : GradientBrush, IRadialGradientBrush
    {
        public RadialGradientBrush(
            IEnumerable<GradientStop> stops,
            Point center,
            Size radii,
            Point focus) : base(stops)
        {
            WpfBrush = new System.Windows.Media.RadialGradientBrush(ConvertStops())
            {
                Center = center,
                GradientOrigin = focus,
                RadiusX = radii.Width,
                RadiusY = radii.Height,
                MappingMode = Space == GradientSpace.Absolute ?
                    BrushMappingMode.Absolute :
                    BrushMappingMode.RelativeToBoundingBox
            };
        }

        public override SpreadMethod SpreadMethod
        {
            get => (SpreadMethod) ((System.Windows.Media.LinearGradientBrush) WpfBrush).SpreadMethod;
            set => ((System.Windows.Media.LinearGradientBrush) WpfBrush).SpreadMethod =
                (GradientSpreadMethod) value;
        }

        protected override void OnStopsChanged(
            object sender,
            NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            ((System.Windows.Media.LinearGradientBrush) WpfBrush).GradientStops = ConvertStops();
        }

        #region IRadialGradientBrush Members

        public float CenterX
        {
            get => (float) ((System.Windows.Media.RadialGradientBrush) WpfBrush).Center.X;
            set
            {
                ((System.Windows.Media.RadialGradientBrush) WpfBrush).Center = new Point(value, CenterY);
                RaisePropertyChanged();
            }
        }

        public float CenterY
        {
            get => (float) ((System.Windows.Media.RadialGradientBrush) WpfBrush).Center.Y;
            set
            {
                ((System.Windows.Media.RadialGradientBrush) WpfBrush).Center = new Point(CenterX, value);
                RaisePropertyChanged();
            }
        }

        public float FocusX
        {
            get => (float) ((System.Windows.Media.RadialGradientBrush) WpfBrush).GradientOrigin.X;
            set
            {
                ((System.Windows.Media.RadialGradientBrush) WpfBrush).GradientOrigin =
                    new Point(value, FocusY);
                RaisePropertyChanged();
            }
        }

        public float FocusY
        {
            get => (float) ((System.Windows.Media.RadialGradientBrush) WpfBrush).GradientOrigin.Y;
            set
            {
                ((System.Windows.Media.RadialGradientBrush) WpfBrush).GradientOrigin =
                    new Point(FocusX, value);
                RaisePropertyChanged();
            }
        }

        public float RadiusX
        {
            get => (float) ((System.Windows.Media.RadialGradientBrush) WpfBrush).RadiusX;
            set
            {
                ((System.Windows.Media.RadialGradientBrush) WpfBrush).RadiusX = value;
                RaisePropertyChanged();
            }
        }

        public float RadiusY
        {
            get => (float) ((System.Windows.Media.RadialGradientBrush) WpfBrush).RadiusY;
            set
            {
                ((System.Windows.Media.RadialGradientBrush) WpfBrush).RadiusY = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}