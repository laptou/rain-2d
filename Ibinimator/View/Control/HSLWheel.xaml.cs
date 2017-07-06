using Ibinimator.Shared;
using System;
using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Screen = System.Windows.Forms.Screen;

namespace Ibinimator.View.Control
{
    public partial class HSLWheel : UserControl, INotifyPropertyChanged
    {
        #region Fields

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color), typeof(HSLWheel), new PropertyMetadata(new Color()));

        public static readonly DependencyProperty HueProperty =
                    DependencyProperty.Register("Hue", typeof(double), typeof(HSLWheel), new PropertyMetadata(0.0, OnColorChanged));

        public static readonly DependencyProperty LightnessProperty =
            DependencyProperty.Register("Lightness", typeof(double), typeof(HSLWheel), new PropertyMetadata(0.0, OnColorChanged));

        public static readonly DependencyProperty SaturationProperty =
            DependencyProperty.Register("Saturation", typeof(double), typeof(HSLWheel), new PropertyMetadata(0.0, OnColorChanged));

        private bool draggingRing;

        private bool draggingTriangle;

        private WriteableBitmap ring;

        private WriteableBitmap triangle;

        #endregion Fields

        #region Constructors

        public HSLWheel()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            PART_ring.MouseDown += OnRingMouseDown;
            PART_triangle.MouseDown += OnTriangleMouseDown;

            UpdateHandles();
        }

        #endregion Constructors

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        #region Properties

        public Color Color
        {
            get
            {
                var x = ColorUtils.HslToRgb(Hue, Saturation, Lightness);
                return Color.FromRgb((byte)(x.r * 255), (byte)(x.g * 255), (byte)(x.b * 255));
            }

            set
            {
                var x = ColorUtils.RgbToHsl(value.R / 255.0, value.G / 255.0, value.B / 255.0);
                Hue = x.h; Saturation = x.s; Lightness = x.l;
                SetValue(ColorProperty, value);
                RaisePropertyChanged(nameof(Color));
            }
        }

        public double Hue
        {
            get { return (double)GetValue(HueProperty); }
            set
            {
                SetValue(HueProperty, (value + 360) % 360);
            }
        }

        public double Lightness
        {
            get { return (double)GetValue(LightnessProperty); }
            set
            {
                SetValue(LightnessProperty, value);
            }
        }

        public double Saturation
        {
            get { return (double)GetValue(SaturationProperty); }
            set
            {
                SetValue(SaturationProperty,
                    MathUtils.Clamp(
                        0,
                        1 - Math.Abs(0.5 - Lightness) * 2,
                        value));
            }
        }

        #endregion Properties

        #region Methods

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            draggingRing = draggingTriangle = false;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            var pos = e.GetPosition(this);

            if (pos.X > ActualWidth || pos.Y > ActualHeight)
            {
                ReleaseMouseCapture();
                draggingRing = draggingTriangle = false;
                return;
            }

            if (draggingRing || draggingTriangle)
                CaptureMouse();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (!draggingRing && !draggingTriangle)
                return;

            var pi2 = Math.PI * 2;
            var pos = e.GetPosition(this);
            pos.Offset(-ActualWidth / 2, -ActualHeight / 2);

            if (draggingRing)
            {
                var rotation = Math.Atan2(pos.Y, pos.X);
                Hue = rotation / pi2 * 360;

                Dispatcher.BeginInvoke((Action)UpdateTriangle, DispatcherPriority.Render, null);
            }

            if (draggingTriangle)
            {
                double height = PART_triangle.ActualWidth / Math.Sqrt(3) * 1.5;

                var tpos = e.GetPosition(PART_triangle);

                Saturation = Math.Max(0, Math.Min(1, 1 - tpos.Y / height));
                Lightness = Math.Max(0, Math.Min(1, tpos.X / PART_triangle.ActualWidth));

                UpdateHandles();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            draggingTriangle = draggingRing = false;

            ReleaseMouseCapture();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            PART_triangle.Width = sizeInfo.NewSize.Width * 0.5 * 0.75 * Math.Sqrt(3);

            UpdateHandles();
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HSLWheel hslWheel)
            {
                hslWheel.RaisePropertyChanged(nameof(Color));
                hslWheel.UpdateHandles();

                if (e.Property == HueProperty)
                    hslWheel.UpdateTriangle();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var pt = PointToScreen(new Point());
            Screen screen = Screen.FromPoint(new System.Drawing.Point((int)pt.X, (int)pt.Y));
            var dpi = screen.GetDpiForMonitor(DpiType.Effective);
            var size = (int)Math.Min(ActualHeight * dpi.y / 96, ActualWidth * dpi.x / 96);

            triangle = new WriteableBitmap(size, (int)(size / MathUtils.SQRT3_2), dpi.x, dpi.y, PixelFormats.Bgra32, null);
            ring = new WriteableBitmap(size, size, dpi.x, dpi.y, PixelFormats.Bgra32, null);

            PART_triangle.Source = triangle;
            PART_ring.Source = ring;

            UpdateTriangle();
            UpdateRing();
        }

        private void OnRingMouseDown(object sender, MouseButtonEventArgs e)
        {
            draggingRing = true;
        }

        private void OnTriangleMouseDown(object sender, MouseButtonEventArgs e)
        {
            draggingTriangle = true;
        }

        private void RaisePropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void UpdateHandles()
        {
            var transform = PART_triangle.RenderTransform as RotateTransform;
            transform.Angle = 90 + Hue;

            int height = (int)(PART_triangle.ActualWidth / Math.Sqrt(3) * 1.5);
            double slope = 1.0 / Math.Sqrt(3);

            var hpos = new Point((Lightness - 0.5) * PART_triangle.ActualWidth, (1 - Saturation) * height);

            hpos.Y = Math.Max(0, Math.Min(height, hpos.Y));
            hpos.X = Math.Max(-slope * hpos.Y, Math.Min(slope * hpos.Y, hpos.X)) + PART_triangle.ActualWidth / 2;

            hpos = PART_triangle.TranslatePoint(hpos, this);

            Canvas.SetLeft(triangleHandle, hpos.X);
            Canvas.SetTop(triangleHandle, hpos.Y);
        }

        private unsafe void UpdateRing()
        {
            ring.Lock();
            byte* pStart = (byte*)(void*)ring.BackBuffer;

            for (int radius = (int)(ring.PixelWidth * 0.375); radius <= ring.PixelWidth * 0.5; radius += 1)
            {
                for (double theta = 0; theta <= MathUtils.PI2; theta += 10f / radius / radius)
                {
                    int row = (int)(radius * Math.Sin(theta)) + ring.PixelHeight / 2;
                    int col = (int)(radius * Math.Cos(theta)) + ring.PixelWidth / 2;
                    int currentPixel = (row * triangle.PixelWidth + col);

                    double h = theta / MathUtils.PI2 * 360;

                    (double r, double g, double b) = ColorUtils.HslToRgb(h, 1f, 0.5f);

                    *(pStart + currentPixel * 4 + 3) = 255; //alpha
                    *(pStart + currentPixel * 4 + 2) = (byte)(r * 255f); //red
                    *(pStart + currentPixel * 4 + 1) = (byte)(g * 255f); //Green
                    *(pStart + currentPixel * 4 + 0) = (byte)(b * 255f); //Blue
                }
            }
            ring.AddDirtyRect(new Int32Rect(0, 0,
                   ring.PixelWidth, ring.PixelHeight));
            ring.Unlock();
        }

        private unsafe void UpdateTriangle()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                int height = (int)(triangle.PixelWidth / Math.Sqrt(3) * 1.5);
                double slope = 1.0 / Math.Sqrt(3);

                triangle.Lock();
                byte* pStart = (byte*)(void*)triangle.BackBuffer;

                for (int iRow = 0; iRow < height; iRow++)
                {
                    int offset = (int)(iRow * slope);

                    for (int iCol = (triangle.PixelWidth / 2) - offset; iCol < (triangle.PixelWidth / 2) + offset; iCol++)
                    {
                        int currentPixel = (iRow * triangle.PixelWidth + iCol);

                        double s = 1f - (double)iRow / height;
                        double l = (double)iCol / triangle.PixelWidth;

                        (double r, double g, double b) = ColorUtils.HslToRgb(Hue, s, l);

                        *(pStart + currentPixel * 4 + 3) = 255; //alpha
                        *(pStart + currentPixel * 4 + 2) = (byte)(r * 255.0); //red
                        *(pStart + currentPixel * 4 + 1) = (byte)(g * 255.0); //Green
                        *(pStart + currentPixel * 4 + 0) = (byte)(b * 255.0); //Blue
                    }
                }
                triangle.AddDirtyRect(new Int32Rect(0, 0,
                       triangle.PixelWidth, height));
                triangle.Unlock();
            }));
        }

        #endregion Methods
    }
}