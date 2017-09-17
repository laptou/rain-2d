using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ibinimator.Shared;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Ibinimator.View.Control
{
    public partial class HslWheel : INotifyPropertyChanged
    {

        private bool _draggingRing;

        private bool _draggingTriangle;

        private WriteableBitmap _ring;

        private WriteableBitmap _triangle;

        public HslWheel()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Ring.MouseDown += OnRingMouseDown;
            Triangle.MouseDown += OnTriangleMouseDown;

            UpdateHandles();
        }

        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            "Color", typeof(Color), typeof(HslWheel), new PropertyMetadata(new Color(), OnDependencyPropertyChanged));

        public Color Color
        {
            get => (Color) GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

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

            if (pos.X > ActualWidth || pos.Y > ActualHeight)
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
            base.OnMouseMove(e);

            if (!_draggingRing && !_draggingTriangle || !IsMouseCaptured)
                return;

            var pi2 = Math.PI * 2;
            var pos = e.GetPosition(this);
            pos.Offset(-ActualWidth / 2, -ActualHeight / 2);

            var (hue, saturation, lightness) = ColorUtils.RgbToHsl(Color.R / 255f, Color.G / 255f, Color.B / 255f);

            if (_draggingRing)
            {
                var rotation = Math.Atan2(pos.Y, pos.X);

                Color = ColorUtils.HslToColor(rotation / pi2 * 360, saturation, lightness);

                Dispatcher.BeginInvoke((Action) UpdateTriangle, DispatcherPriority.Render, null);
            }

            if (_draggingTriangle)
            {
                var height = Triangle.ActualWidth / Math.Sqrt(3) * 1.5;

                var tpos = e.GetPosition(Triangle);

                Color = ColorUtils.HslToColor(hue,
                    Math.Max(0, Math.Min(1, 1 - tpos.Y / height)),
                    Math.Max(0, Math.Min(1, tpos.X / Triangle.ActualWidth)));

                UpdateHandles();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            _draggingTriangle = _draggingRing = false;

            ReleaseMouseCapture();

            RaisePropertyChanged(nameof(Color));
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            Triangle.Width = sizeInfo.NewSize.Width * 0.5 * 0.75 * Math.Sqrt(3);

            UpdateHandles();
        }

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HslWheel hslWheel)
            {
                hslWheel.RaisePropertyChanged(nameof(Color));
                hslWheel.UpdateHandles();
                hslWheel.UpdateTriangle();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (PresentationSource.FromVisual(this) == null)
                return;

            var pt = PointToScreen(new Point());
            var screen = Screen.FromPoint(new System.Drawing.Point((int) pt.X, (int) pt.Y));
            var dpi = screen.GetDpiForMonitor(DpiType.Effective);
            var size = (int) Math.Min(ActualHeight * dpi.y / 96, ActualWidth * dpi.x / 96);

            _triangle = new WriteableBitmap(size, (int) (size / MathUtils.Sqrt3Over2), dpi.x, dpi.y,
                PixelFormats.Bgra32, null);
            _ring = new WriteableBitmap(size, size, dpi.x, dpi.y, PixelFormats.Bgra32, null);

            Triangle.Fill = new ImageBrush(_triangle);
            Ring.Fill = new ImageBrush(_ring);

            UpdateTriangle();
            UpdateRing();
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
            var (hue, saturation, lightness) = ColorUtils.RgbToHsl(Color.R / 255f, Color.G / 255f, Color.B / 255f);

            var transform = (RotateTransform)Triangle.RenderTransform;
            transform.Angle = hue + 90;
            ringRotate.Angle = hue;

            var height = (int) (Triangle.ActualWidth / Math.Sqrt(3) * 1.5);
            var slope = 1.0 / Math.Sqrt(3);

            var hpos = new Point((lightness - 0.5) * Triangle.ActualWidth, (1 - saturation) * height);

            hpos.Y = Math.Max(0, Math.Min(height, hpos.Y));
            hpos.X = Math.Max(-slope * hpos.Y, Math.Min(slope * hpos.Y, hpos.X)) + Triangle.ActualWidth / 2;

            hpos = Triangle.TranslatePoint(hpos, this);

            Canvas.SetLeft(triangleHandle, hpos.X);
            Canvas.SetTop(triangleHandle, hpos.Y);
        }

        private unsafe void UpdateRing()
        {
            _ring.Lock();
            var pStart = (byte*) (void*) _ring.BackBuffer;

            for (var radius = (int) (_ring.PixelWidth * 0.375); radius <= _ring.PixelWidth * 0.5; radius += 1)
            for (double theta = 0; theta <= MathUtils.TwoPi; theta += 10f / radius / radius)
            {
                var row = (int) (radius * Math.Sin(theta)) + _ring.PixelHeight / 2;
                var col = (int) (radius * Math.Cos(theta)) + _ring.PixelWidth / 2;
                var currentPixel = row * _triangle.PixelWidth + col;

                var h = theta / MathUtils.TwoPi * 360;

                (double r, double g, double b) = ColorUtils.HslToRgb(h, 1f, 0.5f);

                *(pStart + currentPixel * 4 + 3) = 255; //alpha
                *(pStart + currentPixel * 4 + 2) = (byte) (r * 255f); //red
                *(pStart + currentPixel * 4 + 1) = (byte) (g * 255f); //Green
                *(pStart + currentPixel * 4 + 0) = (byte) (b * 255f); //Blue
            }
            _ring.AddDirtyRect(new Int32Rect(0, 0,
                _ring.PixelWidth, _ring.PixelHeight));
            _ring.Unlock();
        }

        private unsafe void UpdateTriangle()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var height = (int) (_triangle.PixelWidth / Math.Sqrt(3) * 1.5);
                var slope = 1.0 / Math.Sqrt(3);
                var (hue, _, _) = ColorUtils.RgbToHsl(Color.R / 255f, Color.G / 255f, Color.B / 255f);

                _triangle.Lock();
                var pStart = (byte*) (void*) _triangle.BackBuffer;

                for (var iRow = 0; iRow < height; iRow++)
                {
                    var offset = (int) (iRow * slope);

                    for (var iCol = _triangle.PixelWidth / 2 - offset; iCol < _triangle.PixelWidth / 2 + offset; iCol++)
                    {
                        var currentPixel = iRow * _triangle.PixelWidth + iCol;

                        var s = 1f - (double) iRow / height;
                        var l = (double) iCol / _triangle.PixelWidth;

                        (double r, double g, double b) = ColorUtils.HslToRgb(hue, s, l);

                        *(pStart + currentPixel * 4 + 3) = 255; //alpha
                        *(pStart + currentPixel * 4 + 2) = (byte) (r * 255.0); //red
                        *(pStart + currentPixel * 4 + 1) = (byte) (g * 255.0); //Green
                        *(pStart + currentPixel * 4 + 0) = (byte) (b * 255.0); //Blue
                    }
                }

                _triangle.AddDirtyRect(new Int32Rect(0, 0,
                    _triangle.PixelWidth, height));
                _triangle.Unlock();
            }));
        }
    }
}