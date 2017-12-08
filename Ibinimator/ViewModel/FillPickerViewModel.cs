﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Core.Model;
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
            set => SetRGBA(Red, Green, Blue, value);
        }

        public double Blue
        {
            get => Color.B;
            set => SetRGBA(Red, Green, value, Alpha);
        }

        public Color Color
        {
            get => Get<Color>();
            set => SetRGBA(value.R, value.G, value.B, value.A);
        }

        public double Green
        {
            get => Color.G;
            set => SetRGBA(Red, value, Blue, Alpha);
        }

        public double Hue
        {
            get => _hue;
            set => SetHSLA(value, Saturation, Lightness, Alpha);
        }

        public double Lightness
        {
            get => _lightness;
            set => SetHSLA(Hue, Saturation, value, Alpha);

        }

        public double Red
        {
            get => Color.R;
            set => SetRGBA(value, Green, Blue, Alpha);
        }

        public double Saturation
        {
            get => _saturation;
            set => SetHSLA(Hue, value, Lightness, Alpha);
        }

        public ColorPickerTarget Target { get; }

        private void SetHSLA(double h, double s, double l, double a)
        {
            (_hue, _lightness, _saturation) = (h, s, l);
            Set(ColorUtils.HslaToColor(h, s, l, a), nameof(Color));

            RaisePropertyChanged(nameof(Red));
            RaisePropertyChanged(nameof(Green));
            RaisePropertyChanged(nameof(Blue));
            RaisePropertyChanged(nameof(Hue));
            RaisePropertyChanged(nameof(Saturation));
            RaisePropertyChanged(nameof(Lightness));
        }

        private void SetRGBA(double r, double g, double b, double a)
        {
            Set(ColorUtils.RgbaToColor(r, g, b, a), nameof(Color));
            (_hue, _lightness, _saturation) = ColorUtils.RgbToHsl(r, g, b);

            RaisePropertyChanged(nameof(Red));
            RaisePropertyChanged(nameof(Green));
            RaisePropertyChanged(nameof(Blue));
            RaisePropertyChanged(nameof(Hue));
            RaisePropertyChanged(nameof(Saturation));
            RaisePropertyChanged(nameof(Lightness));
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
                    _fillPicker.Color = fill.Color;

                if (_parent.BrushManager.Stroke?.Brush is SolidColorBrushInfo stroke)
                    _strokePicker.Color = stroke.Color;

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

        public IPenInfo StrokeInfo => _parent.BrushManager.Stroke;

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

            _parent.BrushManager.Apply();

            _updating = false;
        }
    }
}