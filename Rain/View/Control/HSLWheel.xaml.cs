using System;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;

using Rain.Renderer.WPF;
using Rain.Utility;

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Rain.Core.Utility;
using Rain.Native;
using Rain.Renderer.Utility;

using Color = Rain.Core.Model.Color;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Rain.View.Control
{
    public partial class HslWheel : INotifyPropertyChanged
    {
        public static readonly DependencyProperty HueProperty =
            DependencyProperty.Register("Hue",
                                        typeof(double),
                                        typeof(HslWheel),
                                        new PropertyMetadata(0d, OnDependencyPropertyChanged));

        public static readonly DependencyProperty SaturationProperty =
            DependencyProperty.Register("Saturation",
                                        typeof(double),
                                        typeof(HslWheel),
                                        new PropertyMetadata(0d, OnDependencyPropertyChanged));

        public static readonly DependencyProperty LightnessProperty =
            DependencyProperty.Register("Lightness",
                                        typeof(double),
                                        typeof(HslWheel),
                                        new PropertyMetadata(0d, OnDependencyPropertyChanged));

        private bool _draggingRing;
        private bool _draggingTriangle;

        private WriteableBitmap _ring;
        private WriteableBitmap _triangle;
        private int             _size;
        private long _update;

        public HslWheel()
        {
            InitializeComponent();


            Ring.MouseDown += OnRingMouseDown;
            Triangle.MouseDown += OnTriangleMouseDown;
            Loaded += OnLoaded;
        }


        public double Hue
        {
            get => (double) GetValue(HueProperty);
            set => SetValue(HueProperty, value);
        }

        public double Lightness
        {
            get => (double) GetValue(LightnessProperty);
            set => SetValue(LightnessProperty, value);
        }

        public double Saturation
        {
            get => (double) GetValue(SaturationProperty);
            set => SetValue(SaturationProperty, value);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            _draggingRing = _draggingTriangle = false;

            RaisePropertyChanged(nameof(Color));
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            var pos = e.GetPosition(this);

            if (pos.X > ActualWidth ||
                pos.Y > ActualHeight)
            {
                ReleaseMouseCapture();
                _draggingRing = _draggingTriangle = false;

                return;
            }

            if (_draggingRing || _draggingTriangle)
                CaptureMouse();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_draggingRing && !_draggingTriangle ||
                !IsMouseCaptured)
                return;

            var pi2 = MathUtils.TwoPi;
            var pos = e.GetPosition(this);
            pos.Offset(-ActualWidth / 2, -ActualHeight / 2);

            if (_draggingRing)
            {
                var rotation = Math.Atan2(pos.Y, pos.X);
                var hue = (rotation / pi2 * 360 + 360) % 360;

                Hue = hue;

                UpdateHandles();
                UpdateTriangle(hue);
            }

            if (_draggingTriangle)
            {
                var height = Triangle.ActualWidth / Math.Sqrt(3) * 1.5;

                var tpos = e.GetPosition(Triangle).Convert();

                Saturation = Math.Max(0, Math.Min(1, 1 - tpos.Y / height));
                var lightness = Math.Max(0, Math.Min(1, tpos.X / Triangle.ActualWidth));
                var offset = Math.Abs(0.5f - lightness);
                var maxOffset = (1 - Saturation) / 2;
                Lightness = 0.5f - Math.Min(offset, maxOffset) * Math.Sign(0.5f - lightness);

                UpdateHandles();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            _draggingTriangle = _draggingRing = false;

            ReleaseMouseCapture();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (PresentationSource.FromVisual(this) == null)
                return;

            try
            {
                var pt = PointToScreen(new Point());
                var screen = Screen.FromPoint(new System.Drawing.Point((int) pt.X, (int) pt.Y));
                var (x, y) = screen.GetDpiForMonitor(DpiType.Effective);
                _size = (int) Math.Min(ActualHeight * y / 96, ActualWidth * x / 96);

                _triangle = new WriteableBitmap(_size,
                                                (int) (_size / MathUtils.Sqrt3Over2),
                                                x,
                                                y,
                                                PixelFormats.Bgr24,
                                                null);
                _ring = new WriteableBitmap(_size, _size, x, y, PixelFormats.Bgr24, null);

                Triangle.Fill = new ImageBrush(_triangle);
                Ring.Fill = new ImageBrush(_ring);

                UpdateTriangle(Hue);
                UpdateRing();
            }
            catch { }

            Triangle.Width = sizeInfo.NewSize.Width * 0.5 * 0.75 * Math.Sqrt(3);

            UpdateHandles();
        }

        private static void OnDependencyPropertyChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HslWheel hslWheel)
            {
                hslWheel.UpdateHandles();

                if (e.Property == HueProperty)
                    hslWheel.UpdateTriangle(hslWheel.Hue);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            UpdateHandles();
        }

        private void OnRingMouseDown(object sender, MouseButtonEventArgs e)
        {
            _draggingRing = true;
        }

        private void OnTriangleMouseDown(object sender, MouseButtonEventArgs e)
        {
            _draggingTriangle = true;
        }

        private void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void UpdateHandles()
        {
            var (hue, sat, lit) = (Hue, Saturation, Lightness);
            var width = Triangle.ActualWidth;

            TriangleTransform.Angle = hue + 90;
            RingHandleTransform.Angle = hue;

            var height = (int) (width / Math.Sqrt(3) * 1.5);
            var slope = 1.0 / Math.Sqrt(3);

            var hpos = new Point((lit - 0.5) * width, (1 - sat) * height);

            hpos.Y = Math.Max(0, Math.Min(height, hpos.Y));
            hpos.X = Math.Max(-slope * hpos.Y, Math.Min(slope * hpos.Y, hpos.X)) + width / 2;

            hpos = Triangle.TranslatePoint(hpos, this);

            var rcolor = ColorUtils.HslToColor(hue, 1f, 0.5f);
            var tcolor = ColorUtils.HslToColor(hue, sat, lit);

            RingHandleFill.Color = rcolor.Convert();
            TriangleHandleFill.Color = tcolor.Convert();

            Canvas.SetLeft(TriangleHandle, hpos.X);
            Canvas.SetTop(TriangleHandle, hpos.Y);
        }

        private unsafe void UpdateRing()
        {
            _ring.Lock();
            var pStart = (byte*) (void*) _ring.BackBuffer;

            for (var radius = (int) (_ring.PixelWidth * 0.375);
                 radius <= _ring.PixelWidth * 0.5;
                 radius += 1)
                for (double theta = 0; theta <= MathUtils.TwoPi; theta += 10f / radius / radius)
                {
                    var row = (int) (radius * Math.Sin(theta)) + _ring.PixelHeight / 2;
                    var col = (int) (radius * Math.Cos(theta)) + _ring.PixelWidth / 2;
                    var currentPixel = row * _triangle.PixelWidth + col;

                    var h = theta / MathUtils.TwoPi * 360;

                    (double r, double g, double b) = ColorUtils.HslToRgb(h, 1f, 0.5f);

                    *(pStart + currentPixel * 3 + 2) = (byte) (r * 255f); //red
                    *(pStart + currentPixel * 3 + 1) = (byte) (g * 255f); //Green
                    *(pStart + currentPixel * 3 + 0) = (byte) (b * 255f); //Blue
                }

            _ring.AddDirtyRect(new Int32Rect(0, 0, _ring.PixelWidth, _ring.PixelHeight));
            _ring.Unlock();
        }

        private unsafe void UpdateTriangle(double hue)
        {
            var time = Stopwatch.GetTimestamp();

            if (time - _update < Stopwatch.Frequency / 30)
                return;

            _update = time;

            var ptr = SmartPtr.Alloc(_size * _size * 3);
            var pStart = (byte*) (IntPtr) ptr;

            var height = (int) (_size / Math.Sqrt(3) * 1.5);
            var slope = 1.0 / Math.Sqrt(3);

            for (var iRow = 0; iRow < height; iRow++)
            {
                var offset = (int) (iRow * slope);

                for (var iCol = _size / 2 - offset; iCol < _size / 2 + offset; iCol++)
                {
                    var currentPixel = iRow * _size + iCol;

                    var s = 1f - (double) iRow / height;
                    var l = (double) iCol / _size;

                    (double r, double g, double b) = ColorUtils.HslToRgb(hue, s, l);

                    *(pStart + currentPixel * 3 + 2) = (byte) (r * 255.0); //red
                    *(pStart + currentPixel * 3 + 1) = (byte) (g * 255.0); //Green
                    *(pStart + currentPixel * 3 + 0) = (byte) (b * 255.0); //Blue
                }
            }

            Dispatcher.Invoke(() =>
                              {
                                  _triangle.Lock();

                                  NativeHelper.CopyMemory(_triangle.BackBuffer,
                                                          (IntPtr) pStart,
                                                          (uint) (_size * _size * 3));

                                  _triangle.AddDirtyRect(new Int32Rect(0, 0, _size, _size));

                                  _triangle.Unlock();
                              },
                    DispatcherPriority.Render);

            ptr.Dispose();
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}