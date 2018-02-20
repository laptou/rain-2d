using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Rain.Utility;

using System.Threading.Tasks;

using Ibinimator.Renderer.Utility;

using Rain.Core;
using Rain.Core.Model.Paint;

using WPF = System.Windows.Media;

namespace Rain.ViewModel
{
    public enum ColorPickerTarget
    {
        Fill,
        Stroke
    }

    public class ColorViewModel : ViewModel
    {
        private bool   _changing;
        private double _hue, _saturation, _lightness, _alpha = 1;

        public ColorViewModel(IArtContext artContext) { Context = artContext; }

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

                return (Mode == ColorPickerTarget.Fill ? res.Fill : res.Stroke?.Brush) as
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

            Context.InvalidateRender();

            _changing = false;
        }

        private void HistoryManagerOnTraversed(object sender, long l) { Update(); }

        private void OnContextChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Context == null)
                return;

            Context.ToolManager.FillUpdated += ToolManagerOnFillUpdated;
            Context.ToolManager.StrokeUpdated += ToolManagerOnStrokeUpdated;
            Context.HistoryManager.Traversed += HistoryManagerOnTraversed;
        }

        private void OnContextChanging(object sender, PropertyChangingEventArgs e)
        {
            if (Context == null)
                return;

            Context.ToolManager.FillUpdated -= ToolManagerOnFillUpdated;
            Context.ToolManager.StrokeUpdated -= ToolManagerOnStrokeUpdated;
            Context.HistoryManager.Traversed -= HistoryManagerOnTraversed;
        }

        private void ToolManagerOnFillUpdated(object sender, IBrushInfo brushInfo)
        {
            Ui(() =>
               {
                   Fill = brushInfo?.CreateWpfBrush();
                   Update();
               });
        }

        private void ToolManagerOnStrokeUpdated(object sender, IPenInfo penInfo)
        {
            Ui(() =>
               {
                   Stroke = penInfo?.Brush?.CreateWpfBrush();
                   Update();
               });
        }

        private void Update()
        {
            if (!_changing &&
                Current != null)
            {
                (_hue, _saturation, _lightness, _alpha) = ColorUtils.ColorToHsla(Current.Color);
                RaisePropertyChanged(nameof(Hue),
                                     nameof(Saturation),
                                     nameof(Lightness),
                                     nameof(Red),
                                     nameof(Green),
                                     nameof(Blue),
                                     nameof(Alpha));
            }
        }

        #region Brush Properties

        public WPF.Brush Fill
        {
            get => Get<WPF.Brush>();
            set => Set(value);
        }

        public WPF.Brush Stroke
        {
            get => Get<WPF.Brush>();
            set => Set(value);
        }

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