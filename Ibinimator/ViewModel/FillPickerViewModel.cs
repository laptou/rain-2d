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

            public ColorPickerTarget Target { get; }

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
                this._parent = parent;

                parent.Selection.CollectionChanged += OnSelectionChanged;

                _strokePicker.PropertyChanged += OnColorPickerPropertyChanged;
                _fillPicker.PropertyChanged += OnColorPickerPropertyChanged;
            }

            #endregion Constructors

            #region Properties

            public ColorPickerViewModel ColorPicker =>
                _pickerTarget == ColorPickerTarget.Fill ?
                _fillPicker : _strokePicker;

            public Brush FillBrush => _parent.BrushManager.Fill.ToWpf();

            public Brush StrokeBrush => _parent.BrushManager.Stroke.ToWpf();

            #endregion Properties

            #region Methods

            private void OnColorPickerPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                var picker = (ColorPickerViewModel) sender;

                if (picker.Target == ColorPickerTarget.Fill)
                    _parent.BrushManager.Fill = 
                        new BrushInfo(BrushType.Color) { Color = picker.Color.ToDirectX() };
                else
                    _parent.BrushManager.Stroke =
                        new BrushInfo(BrushType.Color) { Color = picker.Color.ToDirectX() };
            }

            private void OnSelectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                RaisePropertyChanged(nameof(FillBrush));
                RaisePropertyChanged(nameof(StrokeBrush));
            }

            #endregion Methods
        }

        #endregion Classes
    }
}