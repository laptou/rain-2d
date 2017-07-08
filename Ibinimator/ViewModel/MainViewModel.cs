using Ibinimator.Model;
using Ibinimator.Shared;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.ComponentModel;

namespace Ibinimator.ViewModel
{
    public class MainViewModel : ViewModel
    {
        public Layer Root { get => Get<Layer>(); set => Set(value); }

        public float Zoom { get => Get<float>(); set => Set(value); }

        public ObservableCollection<Layer> Selection { get; set; } = new ObservableCollection<Layer>();

        public ColorPickerViewModel ColorPicker { get; set; } = new ColorPickerViewModel();

        public MainViewModel()
        {
            Root = new Layer();
            var l = new Layer();
            l.SubLayers.Add(new Layer());
            l.SubLayers.Add(new Layer());
            Root.SubLayers.Add(new Layer());
            Root.SubLayers.Add(new Layer());

            var e = new Ellipse();
            e.X = 100;
            e.Y = 100;
            e.RadiusX = 50;
            e.RadiusY = 50;
            e.FillBrush = new BrushInfo(BrushType.Color) { Color = new RawColor4(1f, 1f, 0, 1f) };
            e.StrokeBrush = new BrushInfo(BrushType.Color) { Color = new RawColor4(1f, 0, 0, 1f) };
            e.StrokeWidth = 5;

            Root.SubLayers.Add(l);
            l.SubLayers.Add(e);

            Zoom = 1;

            Root.PropertyChanged += OnLayerPropertyChanged;
        }

        private void OnLayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Layer layer)
            {
                if(e.PropertyName == nameof(Layer.Selected))
                {
                    if (layer.Selected)
                        Selection.Add(layer);
                    else
                        Selection.Remove(layer);
                }
            }
            else throw new ArgumentException("What?!");
        }
    }

    // use miniature viewmodels for encapsulation
    public class ColorPickerViewModel : ViewModel
    {
        public ColorPickerViewModel()
        {
            Alpha = 1;
            Saturation = 1;
            Lightness = 0.5;
        }

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

        public double Alpha
        {
            get => Get<double>();
            set
            {
                Set(value);
                RaisePropertyChanged(nameof(Color));
            }
        }

        public double Red { get => Color.R / 255f; set => Color = ColorUtils.RgbToColor(value, Green, Blue); }
        public double Green { get => Color.G / 255f; set => Color = ColorUtils.RgbToColor(Red, value, Blue); }
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
    }
}
