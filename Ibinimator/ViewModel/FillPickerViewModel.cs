using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ibinimator.Utility;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Core.Utility;
using Ibinimator.Renderer.Model;
using Ibinimator.Renderer.WPF;
using Ibinimator.Shared;
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
            private double _alpha;
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
                get => _alpha;
                set => Color = ColorUtils.HslaToColor(Hue, Saturation, Lightness, _alpha = value);
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
                    (_hue, _saturation, _lightness, _alpha) = ColorUtils.ColorToHsla(value);

                    RaisePropertyChanged(nameof(Red));
                    RaisePropertyChanged(nameof(Green));
                    RaisePropertyChanged(nameof(Blue));
                    RaisePropertyChanged(nameof(Hue));
                    RaisePropertyChanged(nameof(Saturation));
                    RaisePropertyChanged(nameof(Lightness));
                }
            }

            public void SetColor(Core.Color color)
            {
                Set(color, nameof(Color));

                RaisePropertyChanged(nameof(Red));
                RaisePropertyChanged(nameof(Green));
                RaisePropertyChanged(nameof(Blue));
            }

            public double Green
            {
                get => Color.G;
                set => Color = ColorUtils.RgbaToColor(Red, value, Blue, Alpha);
            }

            public double Hue
            {
                get => _hue;
                set => SetColor(ColorUtils.HslaToColor(_hue = value, Saturation, Lightness, Alpha));
            }

            public double Lightness
            {
                get => _lightness;
                set => SetColor(ColorUtils.HslaToColor(Hue, Saturation, _lightness = value, Alpha));
            }

            public double Red
            {
                get => Color.R;
                set => Color = ColorUtils.RgbaToColor(value, Green, Blue, Alpha);
            }

            public double Saturation
            {
                get => _saturation;
                set => SetColor(ColorUtils.HslaToColor(Hue, _saturation = value, Lightness, Alpha));
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
                        _fillPicker.SetColor(fill.Color);

                    if (_parent.BrushManager.Stroke?.Brush is SolidColorBrushInfo stroke)
                        _strokePicker.SetColor(stroke.Color);

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

            public Brush FillBrush => _parent.BrushManager.Fill?.CreateBrush(new WpfRenderContext()) as Brush;

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

            public Brush StrokeBrush => _parent.BrushManager.Stroke?.Brush?.CreateBrush(new WpfRenderContext()) as Brush;

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

            public float StrokeMiterLimit
            {
                get => StrokeStyle.MiterLimit;
                set
                {
                    var ss = StrokeStyle;
                    ss.MiterLimit = value;
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

            public ObservableList<float> StrokeDashes => _parent.BrushManager.Stroke.Dashes;

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
                //get => _parent.BrushManager.StrokeStyle;
                //set => _parent.BrushManager.StrokeStyle = value;
                get => default;
                set { }
            }

            public float StrokeWidth
            {
                get => _parent.BrushManager.Stroke?.Width ?? 0;
                set => _parent.BrushManager.Stroke.Width = value;
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
                        new SolidColorBrushInfo {Color = picker.Color};
                else
                    _parent.BrushManager.Stroke.Brush =
                        new SolidColorBrushInfo {Color = picker.Color};

                _updating = false;
            }
        }

        #endregion
    }
}