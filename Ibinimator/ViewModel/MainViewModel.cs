using Ibinimator.Model;
using Ibinimator.Shared;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Input;

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

            var l = new Group();

            var e = new Ellipse();
            e.X = 100;
            e.Y = 100;
            e.RadiusX = 50;
            e.RadiusY = 50;
            e.FillBrush = new BrushInfo(BrushType.Color) { Color = new RawColor4(1f, 1f, 0, 1f) };
            e.StrokeBrush = new BrushInfo(BrushType.Color) { Color = new RawColor4(1f, 0, 0, 1f) };
            e.StrokeWidth = 5;

            var r = new Rectangle();
            r.X = 150;
            r.Y = 150;
            r.Width = 100;
            r.Height = 100;
            r.FillBrush = new BrushInfo(BrushType.Color) { Color = new RawColor4(1f, 0, 1f, 1f) };
            r.StrokeBrush = new BrushInfo(BrushType.Color) { Color = new RawColor4(0, 1f, 1f, 1f) };
            r.StrokeWidth = 5;

            Root.Add(l);

            l.Add(e);
            l.Add(r);

            Zoom = 1;

            ColorPicker.PropertyChanged += OnColorPickerPropertyChanged;
            Root.PropertyChanged += OnLayerPropertyChanged;

            SelectLayerCommand = new DelegateCommand(OnSelectLayer);
        }

        private void OnColorPickerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            foreach (var graph in Selection.Select(l => l.Flatten()))
                foreach (var layer in graph)
                {
                    if (layer is Shape shape && shape.FillBrush?.BrushType == BrushType.Color)
                        shape.FillBrush.Color = new SharpDX.Color4(
                            (float)ColorPicker.Red,
                            (float)ColorPicker.Green,
                            (float)ColorPicker.Blue,
                            (float)ColorPicker.Alpha);
                }
        }

        public DelegateCommand SelectLayerCommand { get; }

        private void OnSelectLayer(object param)
        {
            if (param is Layer layer)
            {
                if(Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    layer.Selected = !layer.Selected;
                }
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    if (Selection.Count > 0)
                    {
                        bool inRange = false;

                        foreach (var l in Selection[0].Parent.SubLayers)
                        {
                            if(l == layer || l == Selection[0])
                            {
                                inRange = !inRange;
                            }

                            if(inRange)
                            {
                                l.Selected = true;
                            }
                        }
                    }
                }
                else
                {
                    // use a while loop so that we don't get 'collection modified' exceptions
                    while (Selection.Count > 0)
                        Selection[0].Selected = false;

                    layer.Selected = true;
                }
            }
            else throw new ArgumentException(nameof(param));
        }

        private void OnLayerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Layer layer)
            {
                if (e.PropertyName == nameof(Layer.Selected))
                {
                    var contains = Selection.Contains(layer);

                    if (layer.Selected && !contains)
                        Selection.Add(layer);
                    else if(!layer.Selected && contains)
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