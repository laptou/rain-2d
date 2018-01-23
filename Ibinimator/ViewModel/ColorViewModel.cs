using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

using Ibinimator.Utility;

using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Renderer.Model;
using Ibinimator.Renderer.Utility;

using WPF = System.Windows.Media;

namespace Ibinimator.ViewModel
{
    public enum ColorPickerTarget
    {
        Fill,
        Stroke
    }

    public class ColorViewModel : ViewModel
    {
        private bool   _changing;
        private double _hue, _saturation, _lightness, _alpha;

        public IArtContext Context
        {
            get => Get<IArtContext>();
            set => Set(value, OnContextChanging, OnContextChanged);
        }

        public ColorPickerTarget Mode
        {
            get => Get<ColorPickerTarget>();
            set => Set(value,
                       nameof(Mode),
                       nameof(Hue),
                       nameof(Saturation),
                       nameof(Lightness),
                       nameof(Alpha));
        }

        private SolidColorBrushInfo Current
        {
            get
            {
                if (Context == null) return null;

                var res = Context.BrushManager.Query();

                return (Mode == ColorPickerTarget.Fill ? res.Fill : res.Stroke.Brush) as
                       SolidColorBrushInfo;
            }
        }

        private void ColorChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Context == null) return;

            _changing = true;

            var color = ColorUtils.HslaToColor(Hue, Saturation, Lightness, Alpha);
            var brush = new SolidColorBrushInfo(color);

            if (Mode == ColorPickerTarget.Fill)
            {
                Context.BrushManager.Apply(brush);
            }
            else
            {
                var res = Context.BrushManager.Query();
                var pen = res.Stroke.Clone<IPenInfo>();
                pen.Brush = brush;
                Context.BrushManager.Apply(pen);
            }

            _changing = false;
        }

        private void HistoryManagerOnCollectionChanged(
            object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            RaisePropertyChanged(nameof(Fill), nameof(Stroke));

            if (!_changing)
            {
                (_hue, _saturation, _lightness, _alpha) = ColorUtils.ColorToHsla(Current.Color);
                RaisePropertyChanged(nameof(Hue),
                                     nameof(Saturation),
                                     nameof(Lightness),
                                     nameof(Alpha));
            }
        }

        private void OnContextChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Context == null)
                return;

            Context.HistoryManager.CollectionChanged += HistoryManagerOnCollectionChanged;
        }

        private void OnContextChanging(object sender, PropertyChangingEventArgs e)
        {
            if (Context == null)
                return;

            Context.HistoryManager.CollectionChanged -= HistoryManagerOnCollectionChanged;
        }

        #region Brush Properties

        public WPF.Brush Fill => Context?.BrushManager.Query().Fill.CreateWpfBrush();

        public WPF.Brush Stroke => Context?.BrushManager.Query().Stroke.Brush.CreateWpfBrush();

        #endregion

        #region HSLA Properties

        public double Hue
        {
            get => _hue;
            set => Set(value, out _hue, ColorChanged);
        }

        public double Saturation
        {
            get => _saturation;
            set => Set(value, out _saturation, ColorChanged);
        }

        public double Lightness
        {
            get => _lightness;
            set => Set(value, out _lightness, ColorChanged);
        }

        public double Alpha
        {
            get => _alpha;
            set => Set(value, out _alpha, ColorChanged);
        }

        #endregion

        #region RGBA Properties

        public double Red
        {
            get => ColorUtils.HslToRgb(Hue, Saturation, Lightness).Red;
            set => (Hue, Saturation, Lightness) = ColorUtils.RgbToHsl(value, Green, Blue);
        }

        public double Green
        {
            get => ColorUtils.HslToRgb(Hue, Saturation, Lightness).Green;
            set => (Hue, Saturation, Lightness) = ColorUtils.RgbToHsl(Red, value, Blue);
        }

        public double Blue
        {
            get => ColorUtils.HslToRgb(Hue, Saturation, Lightness).Blue;
            set => (Hue, Saturation, Lightness) = ColorUtils.RgbToHsl(Red, Green, value);
        }

        #endregion
    }
}