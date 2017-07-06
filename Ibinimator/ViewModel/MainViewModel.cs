using Ibinimator.Model;
using Ibinimator.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Ibinimator.ViewModel
{
    public class MainViewModel : ViewModel
    {
        public Layer Root { get; set; }

        public ColorPickerViewModel ColorPicker { get; set; } = new ColorPickerViewModel();

        public MainViewModel()
        {
            Root = new Layer();
            var l = new Layer();
            l.SubLayers.Add(new Layer());
            l.SubLayers.Add(new Layer());
            Root.SubLayers.Add(new Layer());
            Root.SubLayers.Add(new Layer());
            Root.SubLayers.Add(l);
        }
    }

    // use miniature viewmodels for encapsulation
    public class ColorPickerViewModel : ViewModel
    {
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
        public double Green { get => Color.G / 255f; set => Color = ColorUtils.RgbToColor(Red, value, Blue); }
        public double Blue { get => Color.B / 255f; set => Color = ColorUtils.RgbToColor(Red, Green, value); }

        public Color Color
        {
            get => ColorUtils.HslToColor(Hue, Saturation, Lightness);
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
    }
}
