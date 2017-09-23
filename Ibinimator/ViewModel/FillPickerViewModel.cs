using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ibinimator.Shared;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Ibinimator.Model;
using Ibinimator.Utility;
using SharpDX.Direct2D1;
using Brush = System.Windows.Media.Brush;
using DashStyle = SharpDX.Direct2D1.DashStyle;

namespace Ibinimator.ViewModel
{
    public enum ColorPickerTarget
    {
        Fill,
        Stroke
    }

    public partial class MainViewModel
    {
        #region Nested type: ColorPickerViewModel

        public class ColorPickerViewModel : ViewModel
        {
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
                get => Get<double>();
                set
                {
                    Set(value);
                    RaisePropertyChanged(nameof(Color));
                }
            }

            public double Blue
            {
                get => Color.B / 255f;
                set => Color = ColorUtils.RgbToColor(Red, Green, value);
            }

            public Color Color
            {
                get => Get<Color>();
                set
                {
                    Set(value);
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
                get => Color.G / 255f;
                set => Color = ColorUtils.RgbToColor(Red, value, Blue);
            }

            public double Hue
            {
                get => ColorUtils.RgbToHsl(Color.R / 255f, Color.G / 255f, Color.B / 255f).h;
                set => Color = ColorUtils.HslaToColor(value, Saturation, Lightness, Alpha);
            }

            public double Lightness
            {
                get => ColorUtils.RgbToHsl(Color.R / 255f, Color.G / 255f, Color.B / 255f).l;
                set => Color = ColorUtils.HslaToColor(Hue, Saturation, value, Alpha);
            }

            public double Red
            {
                get => Color.R / 255f;
                set => Color = ColorUtils.RgbToColor(value, Green, Blue);
            }

            public double Saturation
            {
                get => ColorUtils.RgbToHsl(Color.R / 255f, Color.G / 255f, Color.B / 255f).s;
                set => Color = ColorUtils.HslaToColor(Hue, value, Lightness, Alpha);
            }

            public ColorPickerTarget Target { get; }
        }

        #endregion

        #region Nested type: FillPickerViewModel

        public class FillPickerViewModel : ViewModel
        {
            private readonly ColorPickerViewModel _fillPicker = new ColorPickerViewModel(ColorPickerTarget.Fill);
            private readonly MainViewModel _parent;
            private readonly ColorPickerViewModel _strokePicker = new ColorPickerViewModel(ColorPickerTarget.Stroke);

            private bool _updating;

            public FillPickerViewModel(MainViewModel parent)
            {
                _parent = parent;

                _parent.BrushUpdated += (sender, args) =>
                {
                    _updating = true;

                    if (_parent.BrushManager.Fill is SolidColorBrushInfo fill)
                        _fillPicker.Color = fill.Color.ToWpf();

                    if (_parent.BrushManager.Stroke is SolidColorBrushInfo stroke)
                        _strokePicker.Color = stroke.Color.ToWpf();

                    RaisePropertyChanged(nameof(FillBrush));
                    RaisePropertyChanged(nameof(StrokeBrush));
                    RaisePropertyChanged(nameof(StrokeWidth));
                    RaisePropertyChanged(nameof(StrokeStyle));
                    RaisePropertyChanged(nameof(StrokeDash));
                    RaisePropertyChanged(nameof(StrokeCap));
                    RaisePropertyChanged(nameof(StrokeJoin));
                    RaisePropertyChanged(nameof(StrokeDashes));

                    _updating = false;
                };

                _strokePicker.PropertyChanged += OnColorPickerPropertyChanged;
                _fillPicker.PropertyChanged += OnColorPickerPropertyChanged;
            }

            public Brush FillBrush => _parent.BrushManager.Fill?.ToWpf();

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

            public Brush StrokeBrush => _parent.BrushManager.Stroke?.ToWpf();

            public CapStyle StrokeCap
            {
                get => StrokeStyle.StartCap;
                set
                {
                    var ss = StrokeStyle;
                    ss.StartCap = value;
                    ss.EndCap = value;
                    ss.DashCap = value;
                    StrokeStyle = ss;
                }
            }

            public DashStyle StrokeDash
            {
                get => StrokeStyle.DashStyle;
                set
                {
                    var ss = StrokeStyle;
                    ss.DashStyle = value;
                    StrokeStyle = ss;
                }
            }

            public ObservableList<float> StrokeDashes => _parent.BrushManager.StrokeDashes;

            public LineJoin StrokeJoin
            {
                get => StrokeStyle.LineJoin;
                set
                {
                    var ss = StrokeStyle;
                    ss.LineJoin = value;
                    if (value == LineJoin.Miter)
                        ss.MiterLimit = StrokeWidth;
                    StrokeStyle = ss;
                }
            }

            public StrokeStyleProperties1 StrokeStyle
            {
                get => _parent.BrushManager.StrokeStyle;
                set => _parent.BrushManager.StrokeStyle = value;
            }

            public float StrokeWidth
            {
                get => _parent.BrushManager.StrokeWidth;
                set => _parent.BrushManager.StrokeWidth = value;
            }

            private void OnColorPickerPropertyChanged(object sender, PropertyChangedEventArgs e)
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
                        new SolidColorBrushInfo {Color = picker.Color.ToDirectX()};
                else
                    _parent.BrushManager.Stroke =
                        new SolidColorBrushInfo {Color = picker.Color.ToDirectX()};

                _updating = false;
            }
        }

        #endregion
    }
}