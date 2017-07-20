using Ibinimator.Model;
using Ibinimator.Shared;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.ComponentModel;


using System.Windows.Input;
using System.Windows.Media;

namespace Ibinimator.ViewModel
{
    public enum ColorPickerTarget
    {
        Fill, Stroke
    }

    // use miniature viewmodels for encapsulation
    public class ColorPickerViewModel : ViewModel
    {
        #region Constructors

        public ColorPickerViewModel()
        {
            Alpha = 1;
            Saturation = 1;
            Lightness = 0.5;
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
    }

    public class MainViewModel : ViewModel
    {
        #region Constructors

        public MainViewModel()
        {
            Root = new Layer();

            ColorPicker.PropertyChanged += OnColorPickerPropertyChanged;
            Root.PropertyChanged += OnLayerPropertyChanged;

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
            //r.Scale = new SharpDX.Vector2(2, 1);
            r.RotationSkew = SharpDX.Matrix3x2.Rotation(SharpDX.MathUtil.PiOverFour, r.GetBounds().Center);

            Root.Add(l);

            l.Add(e);
            l.Add(r);

            Zoom = 1;

            SelectLayerCommand = new DelegateCommand(OnSelectLayer);
        }

        #endregion Constructors

        #region Properties

        public ColorPickerViewModel ColorPicker { get; set; } = new ColorPickerViewModel();

        public ColorPickerTarget ColorPickerTarget { get => Get<ColorPickerTarget>(); set => Set(value); }

        public Layer Root { get => Get<Layer>(); set => Set(value); }

        public Brush FillBrush
        {
            get
            {
                if (Selection.Count == 1 && Selection[0] is Shape shape)
                    return shape.FillBrush?.ToWPF();
                else
                    return null;
            }
        }

        public Brush StrokeBrush
        {
            get
            {
                if (Selection.Count == 1 && Selection[0] is Shape shape)
                    return shape.StrokeBrush?.ToWPF();
                else
                    return null;
            }
        }

        public ObservableCollection<Layer> Selection { get; set; } = new ObservableCollection<Layer>();

        public DelegateCommand SelectLayerCommand { get; }

        public float Zoom { get => Get<float>(); set => Set(value); }

        #endregion Properties

        #region Methods

        private void OnColorPickerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var color = new SharpDX.Color4(
                (float)ColorPicker.Red,
                (float)ColorPicker.Green,
                (float)ColorPicker.Blue,
                (float)ColorPicker.Alpha);

            foreach (var graph in Selection.Select(l => l.Flatten()))
                foreach (var layer in graph)
                {
                    if (layer is Shape shape) {
                        switch (ColorPickerTarget)
                        {
                            case ColorPickerTarget.Fill:
                                if (shape.FillBrush?.BrushType == BrushType.Color)
                                    shape.FillBrush.Color = color;
                                else
                                    shape.FillBrush = new BrushInfo(BrushType.Color) { Color = color };
                                break;
                            case ColorPickerTarget.Stroke:
                                if (shape.StrokeBrush?.BrushType == BrushType.Color)
                                    shape.StrokeBrush.Color = color;
                                else
                                    shape.StrokeBrush = new BrushInfo(BrushType.Color) { Color = color };
                                break;
                            default:
                                break;
                        }
                    }
                }
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
                    else if (!layer.Selected && contains)
                        Selection.Remove(layer);
                }

                if(sender is Shape shape && shape.Selected)
                {
                    if(e.PropertyName == nameof(Shape.FillBrush))
                        RaisePropertyChanged(nameof(FillBrush));

                    if (e.PropertyName == nameof(Shape.StrokeBrush))
                        RaisePropertyChanged(nameof(StrokeBrush));
                }
            }
            else throw new ArgumentException("What?!");
        }

        private void OnSelectLayer(object param)
        {
            if (param is Layer layer)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
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
                            if (l == layer || l == Selection[0])
                            {
                                inRange = !inRange;
                            }

                            if (inRange)
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

        #endregion Methods
    }
}