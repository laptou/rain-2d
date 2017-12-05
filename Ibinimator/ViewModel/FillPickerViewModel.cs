using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.Renderer.Utility;
using Ibinimator.Renderer.WPF;
using WPF = System.Windows.Media;

namespace Ibinimator.ViewModel
{
    public enum ColorPickerTarget
    {
        Fill,
        Stroke
    }

    public class ColorPickerViewModel : ViewModel
    {
        private double _hue;
        private double _lightness;
        private double _saturation;

        public bool Flag;

        public ColorPickerViewModel(ColorPickerTarget target)
        {
            Alpha = 1;
            Saturation = 1;
            Lightness = 0.5;
            Target = target;
        }

        public double Alpha
        {
            get => Color.A;
            set => Color = ColorUtils.HslaToColor(Hue, Saturation, Lightness, value);
        }

        public double Blue
        {
            get => Color.B;
            set => Color = ColorUtils.RgbaToColor(Red, Green, value, Alpha);
        }

        public Color Color
        {
            get => Get<Color>();
            set
            {
                Set(value);
                (_hue, _saturation, _lightness, _) = ColorUtils.ColorToHsla(value);

                RaisePropertyChanged(nameof(Red));
                RaisePropertyChanged(nameof(Green));
                RaisePropertyChanged(nameof(Blue));
                RaisePropertyChanged(nameof(Hue));
                RaisePropertyChanged(nameof(Saturation));
                RaisePropertyChanged(nameof(Lightness));
            }
        }

        public double Green
        {
            get => Color.G;
            set => Color = ColorUtils.RgbaToColor(Red, value, Blue, Alpha);
        }

        public double Hue
        {
            get => _hue;
            set => SetColor(
                ColorUtils.HslaToColor(_hue = value, Saturation, Lightness, Alpha));
        }

        public double Lightness
        {
            get => _lightness;
            set => SetColor(
                ColorUtils.HslaToColor(Hue, Saturation, _lightness = value, Alpha));
        }

        public double Red
        {
            get => Color.R;
            set => Color = ColorUtils.RgbaToColor(value, Green, Blue, Alpha);
        }

        public double Saturation
        {
            get => _saturation;
            set => SetColor(
                ColorUtils.HslaToColor(Hue, _saturation = value, Lightness, Alpha));
        }

        public ColorPickerTarget Target { get; }

        public void SetColor(Color color)
        {
            Set(color, nameof(Color));

            RaisePropertyChanged(nameof(Red));
            RaisePropertyChanged(nameof(Green));
            RaisePropertyChanged(nameof(Blue));
            RaisePropertyChanged(nameof(Alpha));
        }
    }

    public class FillPickerViewModel : ViewModel
    {
        private readonly ColorPickerViewModel _fillPicker =
            new ColorPickerViewModel(ColorPickerTarget.Fill);

        private readonly MainViewModel _parent;

        private readonly ColorPickerViewModel _strokePicker =
            new ColorPickerViewModel(ColorPickerTarget.Stroke);

        private bool _updating;

        public FillPickerViewModel(MainViewModel parent, IBrushManager brushManager)
        {
            _parent = parent;

            brushManager.PropertyChanged += (sender, args) =>
            {
                _updating = true;

                if (_parent.BrushManager.Fill is SolidColorBrushInfo fill)
                    _fillPicker.SetColor(fill.Color);

                if (_parent.BrushManager.Stroke?.Brush is SolidColorBrushInfo stroke)
                    _strokePicker.SetColor(stroke.Color);

                RaisePropertyChanged(nameof(FillBrush));
                RaisePropertyChanged(nameof(StrokeBrush));
                RaisePropertyChanged(nameof(StrokeInfo));

                _updating = false;
            };

            _strokePicker.PropertyChanged += OnColorPickerPropertyChanged;
            _fillPicker.PropertyChanged += OnColorPickerPropertyChanged;
        }

        public WPF.Brush FillBrush => _parent
            .BrushManager.Fill?
            .CreateBrush(new WpfRenderContext())
            .Unwrap<WPF.Brush>();

        public ColorPickerViewModel Picker =>
            PickerTarget == ColorPickerTarget.Fill ? _fillPicker : _strokePicker;

        public ColorPickerTarget PickerTarget
        {
            get => Get<ColorPickerTarget>();
            set
            {
                Set(value);
                RaisePropertyChanged(nameof(Picker));
            }
        }

        public WPF.Brush StrokeBrush =>
            StrokeInfo?.Brush?
            .CreateBrush(new WpfRenderContext())
            .Unwrap<WPF.Brush>();

        public PenInfo StrokeInfo => _parent.BrushManager.Stroke;

        private void OnColorPickerPropertyChanged(
            object sender,
            PropertyChangedEventArgs e)
        {
            if (_updating) return;

            if (e.PropertyName != nameof(ColorPickerViewModel.Color)) return;

            _updating = true;

            var picker = (ColorPickerViewModel) sender;

            lock (picker)
            {
                if (picker.Flag)
                    return;
            }

            if (picker.Target == ColorPickerTarget.Fill)
                _parent.BrushManager.Fill =
                    new SolidColorBrushInfo {Color = picker.Color};
            else
                _parent.BrushManager.Stroke.Brush =
                    new SolidColorBrushInfo {Color = picker.Color};

            _updating = false;
        }
    }
}