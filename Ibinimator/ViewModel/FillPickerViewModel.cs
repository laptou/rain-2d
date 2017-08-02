using Ibinimator.Model;
using Ibinimator.Shared;
using Ibinimator.View.Control;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Ibinimator.ViewModel
{
    public partial class MainViewModel
    {
        #region Enums

        public enum ColorPickerTarget
        {
            Fill, Stroke
        }

        #endregion Enums

        #region Classes

        public class ColorPickerViewModel : ViewModel
        {
            #region Constructors

            public ColorPickerViewModel(ColorPickerTarget target)
            {
                Alpha = 1;
                Saturation = 1;
                Lightness = 0.5;
                Target = target;
            }

            #endregion Constructors

            #region Properties

            public double Alpha
            {
                get => Get<double>();
                set
                {
                    Set(value);
                    RaisePropertyChanged(nameof(Color));
                }
            }

            public double Blue { get => Color.B / 255f; set => Color = ColorUtils.RgbToColor(Red, Green, value); }

            public Color Color 
            {
                get => ColorUtils.HslaToColor(Hue, Saturation, Lightness, Alpha);
                set => (Hue, Saturation, Lightness, Alpha) = ColorUtils.RgbaToHsla(value.R / 255f, value.G / 255f, value.B / 255f, value.A / 255f);
            }

            public double Green { get => Color.G / 255f; set => Color = ColorUtils.RgbToColor(Red, value, Blue); }

            public double Hue
            {
                get => Get<double>();
                set
                {
                    Set(value);
                    RaisePropertyChanged(nameof(Color));
                    RaisePropertyChanged(nameof(Red));
                    RaisePropertyChanged(nameof(Green));
                    RaisePropertyChanged(nameof(Blue));
                }
            }

            public double Lightness
            {
                get => Get<double>();
                set
                {
                    Set(value);
                    RaisePropertyChanged(nameof(Color));
                    RaisePropertyChanged(nameof(Red));
                    RaisePropertyChanged(nameof(Green));
                    RaisePropertyChanged(nameof(Blue));
                }
            }

            public double Red { get => Color.R / 255f; set => Color = ColorUtils.RgbToColor(value, Green, Blue); }

            public double Saturation
            {
                get => Get<double>();
                set
                {
                    Set(value);
                    RaisePropertyChanged(nameof(Color));
                    RaisePropertyChanged(nameof(Red));
                    RaisePropertyChanged(nameof(Green));
                    RaisePropertyChanged(nameof(Blue));
                }
            }

            public ColorPickerTarget Target { get; }

            public bool Flag = false;

            #endregion Properties
        }

        public class FillPickerViewModel : ViewModel
        {
            #region Fields

            private ColorPickerViewModel _fillPicker = new ColorPickerViewModel(ColorPickerTarget.Fill);
            private MainViewModel _parent;

            private ColorPickerTarget _pickerTarget = ColorPickerTarget.Fill;
            private ColorPickerViewModel _strokePicker = new ColorPickerViewModel(ColorPickerTarget.Stroke);

            #endregion Fields

            #region Constructors

            public FillPickerViewModel(MainViewModel parent)
            {
                _parent = parent;

                _parent.BrushUpdated += (sender, args) =>
                {
                    RaisePropertyChanged(nameof(FillBrush));
                    RaisePropertyChanged(nameof(StrokeBrush));

                    if (_parent.BrushManager.Fill is SolidColorBrushInfo fill)
                    {
                        lock(_fillPicker)
                            _fillPicker.Flag = true;

                        if(_fillPicker.Flag)
                            _fillPicker.Color = fill.Color.ToWpf();

                        lock (_fillPicker)
                            _fillPicker.Flag = false;
                    }
                };

                _strokePicker.PropertyChanged += OnColorPickerPropertyChanged;
                _fillPicker.PropertyChanged += OnColorPickerPropertyChanged;
            }

            #endregion Constructors

            #region Properties

            public ColorPickerViewModel ColorPicker =>
                _pickerTarget == ColorPickerTarget.Fill ?
                _fillPicker : _strokePicker;

            public Brush FillBrush => _parent.BrushManager.Fill?.ToWpf();

            public Brush StrokeBrush => _parent.BrushManager.Stroke?.ToWpf();

            #endregion Properties

            #region Methods

            private void OnColorPickerPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                var picker = (ColorPickerViewModel) sender;

                lock (picker)
                {
                    if (picker.Flag)
                        return;
                }

                if (picker.Target == ColorPickerTarget.Fill)
                    _parent.BrushManager.Fill = 
                        new SolidColorBrushInfo { Color = picker.Color.ToDirectX() };
                else
                    _parent.BrushManager.Stroke =
                        new SolidColorBrushInfo { Color = picker.Color.ToDirectX() };
            }

            #endregion Methods
        }

        #endregion Classes
    }
}