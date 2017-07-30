using Ibinimator.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Ibinimator.ViewModel
{
    public partial class MainViewModel
    {
        public enum ColorPickerTarget
        {
            Fill, Stroke
        }

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
                set
                {
                    (Hue, Saturation, Lightness) = ColorUtils.RgbToHsl(value.R / 255f, value.G / 255f, value.B / 255f);
                    RaisePropertyChanged(nameof(Red));
                    RaisePropertyChanged(nameof(Green));
                    RaisePropertyChanged(nameof(Blue));
                    RaisePropertyChanged(nameof(Hue));
                    RaisePropertyChanged(nameof(Saturation));
                    RaisePropertyChanged(nameof(Lightness));
                }
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

            #endregion Properties

            public ColorPickerTarget Target { get; }
        }

        public class FillPickerViewModel : ViewModel
        {
            ColorPickerTarget pickerTarget = ColorPickerTarget.Fill;
            ColorPickerViewModel strokePicker = new ColorPickerViewModel(ColorPickerTarget.Stroke);
            ColorPickerViewModel fillPicker = new ColorPickerViewModel(ColorPickerTarget.Fill);

            public FillPickerViewModel()
            {
                strokePicker.PropertyChanged += OnColorPickerPropertyChanged;
                fillPicker.PropertyChanged += OnColorPickerPropertyChanged;
            }

            public ColorPickerViewModel ColorPicker =>
                pickerTarget == ColorPickerTarget.Fill ?
                fillPicker : strokePicker;

            private void OnColorPickerPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                var color = new SharpDX.Color4(
                    (float)ColorPicker.Red,
                    (float)ColorPicker.Green,
                    (float)ColorPicker.Blue,
                    (float)ColorPicker.Alpha);
            }

            public Brush FillBrush { get => Get<Brush>(); set => Set(value); }

            public Brush StrokeBrush { get => Get<Brush>(); set => Set(value); }
        }
    }
}
