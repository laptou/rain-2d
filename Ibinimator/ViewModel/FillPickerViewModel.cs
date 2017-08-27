using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Ibinimator.Shared;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Ibinimator.Model;
using Ibinimator.Service;
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
                get => ColorUtils.HslaToColor(Hue, Saturation, Lightness, Alpha);
                set => (Hue, Saturation, Lightness, Alpha) =
                    ColorUtils.RgbaToHsla(value.R / 255f, value.G / 255f, value.B / 255f, value.A / 255f);
            }

            public double Green
            {
                get => Color.G / 255f;
                set => Color = ColorUtils.RgbToColor(Red, value, Blue);
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

            public double Red
            {
                get => Color.R / 255f;
                set => Color = ColorUtils.RgbToColor(value, Green, Blue);
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

            public ColorPickerTarget Target { get; }
        }

        #endregion

        #region Nested type: FillPickerViewModel

        public class FillPickerViewModel : ViewModel
        {
            private readonly ColorPickerViewModel _fillPicker = new ColorPickerViewModel(ColorPickerTarget.Fill);
            private readonly MainViewModel _parent;

            private readonly ColorPickerViewModel _strokePicker = new ColorPickerViewModel(ColorPickerTarget.Stroke);

            public FillPickerViewModel(MainViewModel parent)
            {
                _parent = parent;

                _parent.BrushUpdated += (sender, args) =>
                {
                    switch (args.PropertyName)
                    {
                        case nameof(IBrushManager.Fill):
                            RaisePropertyChanged(nameof(FillBrush));

                            if (_parent.BrushManager.Fill is SolidColorBrushInfo fill)
                            {
                                lock (_fillPicker)
                                {
                                    _fillPicker.Flag = true;
                                }

                                if (_fillPicker.Flag)
                                    _fillPicker.Color = fill.Color.ToWpf();

                                lock (_fillPicker)
                                {
                                    _fillPicker.Flag = false;
                                }
                            }
                            break;
                        case nameof(IBrushManager.Stroke):
                            RaisePropertyChanged(nameof(StrokeBrush));

                            if (_parent.BrushManager.Stroke is SolidColorBrushInfo stroke)
                            {
                                lock (_strokePicker)
                                {
                                    _strokePicker.Flag = true;
                                }

                                if (_strokePicker.Flag)
                                    _strokePicker.Color = stroke.Color.ToWpf();

                                lock (_strokePicker)
                                {
                                    _strokePicker.Flag = false;
                                }
                            }
                            break;

                        case nameof(IBrushManager.StrokeWidth):
                            RaisePropertyChanged(nameof(StrokeWidth));
                            break;

                        case nameof(IBrushManager.StrokeStyle):
                            RaisePropertyChanged(nameof(StrokeStyle));
                            RaisePropertyChanged(nameof(StrokeDash));
                            RaisePropertyChanged(nameof(StrokeCap));
                            RaisePropertyChanged(nameof(StrokeJoin));
                            break;

                        case nameof(IBrushManager.StrokeDashes):
                            RaisePropertyChanged(nameof(StrokeDashes));
                            break;
                    }
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

            public ObservableCollection<float> StrokeDashes => _parent.BrushManager.StrokeDashes;

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
            }
        }

        #endregion
    }
}