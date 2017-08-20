using Ibinimator.Shared;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using D2D = SharpDX.Direct2D1;
using D3D9 = SharpDX.Direct3D9;
using SharpDX;
using Device = SharpDX.Direct3D11.Device;

namespace Ibinimator.View.Control
{
    public abstract class D2DImage : Image
    {
        #region Fields

        private static readonly DependencyPropertyKey FpsPropertyKey = DependencyProperty.RegisterReadOnly(
           "FPS",
           typeof(int),
           typeof(D2DImage),
           new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.None)
           );

        public static readonly DependencyProperty FpsProperty = FpsPropertyKey.DependencyProperty;

        public RenderMode RenderMode
        {
            get { return (RenderMode)GetValue(RenderModeProperty); }
            set { SetValue(RenderModeProperty, value); }
        }

        public static readonly DependencyProperty RenderModeProperty =
            DependencyProperty.Register("RenderMode", typeof(RenderMode), typeof(D2DImage), new PropertyMetadata(RenderMode.Constant, OnRenderModeChanged));

        private static void OnRenderModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is D2DImage image)
            {
                if (e.NewValue != e.OldValue && (RenderMode)e.NewValue == RenderMode.Manual)
                    CompositionTarget.Rendering -= image.OnRendering;
            }
        }

        private readonly Stopwatch _renderTimer = new Stopwatch();
        private D2D.Factory _d2DFactory;
        private D2D.RenderTarget _d2DRenderTarget;
        private Device _device;
        private int _frameCount = 0;
        private Queue<int> _frameCountHist = new Queue<int>();
        private int _frameCountHistTotal = 0;
        private long _lastFrameTime = 0;
        private long _lastRenderTime = 0;
        private bool _invalidated = false;
        private Stack<Int32Rect> _dirty = new Stack<Int32Rect>();
        private Texture2D _renderTarget;
        private Dx11ImageSource _surface;

        #endregion Fields

        #region Constructors

        protected D2DImage()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            if (App.IsDesigner) return;

            StartD3D();

            Stretch = Stretch.Fill;
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        }

        #endregion Constructors

        #region Properties

        public int Fps
        {
            get { return (int)GetValue(FpsProperty); }
            protected set { SetValue(FpsPropertyKey, value); }
        }

        public bool EnableAntialiasing
        {
            get => _d2DRenderTarget?.AntialiasMode == D2D.AntialiasMode.Aliased;
            set => _d2DRenderTarget.AntialiasMode = value ? D2D.AntialiasMode.PerPrimitive : D2D.AntialiasMode.Aliased;
        }

        public Dx11ImageSource Surface => _surface;

        public Device Device => _device;

        #endregion Properties

        public event EventHandler<D2D.RenderTarget> RenderTargetBound;

        #region Methods

        protected abstract void Render(D2D.RenderTarget target);

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            CreateAndBindTargets();
            PrepareAndCallRender();
            _surface.InvalidateD3DImage();
        }

        private void CalcFps()
        {
            _frameCount++;
            if (_renderTimer.ElapsedMilliseconds - _lastFrameTime > 1000)
            {
                _frameCountHist.Enqueue(_frameCount);
                _frameCountHistTotal += _frameCount;
                if (_frameCountHist.Count > 5)
                {
                    _frameCountHistTotal -= _frameCountHist.Dequeue();
                }

                Dispatcher.InvokeAsync(() => Fps = _frameCountHistTotal / _frameCountHist.Count);

                _frameCount = 0;
                _lastFrameTime = _renderTimer.ElapsedMilliseconds;
            }
        }

        private void CreateAndBindTargets()
        {
            this._surface.SetRenderTarget(null);

            Disposer.SafeDispose(ref _d2DRenderTarget);
            Disposer.SafeDispose(ref _d2DFactory);
            Disposer.SafeDispose(ref _renderTarget);

            _d2DFactory = new D2D.Factory(D2D.FactoryType.MultiThreaded);

            var width = (int)Math.Max(1, ActualWidth * _d2DFactory.DesktopDpi.Width / 96.0);
            var height = (int)Math.Max(1, ActualHeight * _d2DFactory.DesktopDpi.Height / 96.0);

            var renderDesc = new Texture2DDescription
            {
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Width = width,
                Height = height,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.Shared,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1
            };

            _renderTarget = new Texture2D(_device, renderDesc);

            var dxgiSurface = _renderTarget.QueryInterface<SharpDX.DXGI.Surface>();
            var dxgiDevice = _device.QueryInterface<SharpDX.DXGI.Device1>();

            var dxgiFactory = dxgiDevice.Adapter.GetParent<SharpDX.DXGI.Factory1>();

            var rtp = new D2D.RenderTargetProperties(new D2D.PixelFormat(SharpDX.DXGI.Format.Unknown, D2D.AlphaMode.Premultiplied));

            _d2DRenderTarget = new D2D.RenderTarget(_d2DFactory, dxgiSurface, rtp) { DotsPerInch = _d2DFactory.DesktopDpi};

            _device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height, 0.0f, 1.0f);

            this._surface.SetRenderTarget(_renderTarget);

            RenderTargetBound?.Invoke(this, _d2DRenderTarget);
        }

        private void EndD3D()
        {
            _surface.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
            Source = null;

            Disposer.SafeDispose(ref _d2DRenderTarget);
            Disposer.SafeDispose(ref _d2DFactory);
            Disposer.SafeDispose(ref _surface);
            Disposer.SafeDispose(ref _renderTarget);
            Disposer.SafeDispose(ref _device);
        }

        private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_surface.IsFrontBufferAvailable)
            {
                StartRendering();
            }
            else
            {
                StopRendering();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if(App.IsDesigner) return;

            PrepareAndCallRender();
            _surface.InvalidateD3DImage();
        }

        private async void OnRendering(object sender, EventArgs e)
        {
            if (!_renderTimer.IsRunning)
            {
                return;
            }

            if (RenderMode == RenderMode.Constant || _invalidated)
            {
                await Task.Run((Action)PrepareAndCallRender);

                Int32Rect rect;
                _surface.Lock();
                while (_dirty.Count > 0)
                    _surface.AddDirtyRect(rect = _dirty.Pop());
                _surface.Unlock();

                _lastRenderTime = _renderTimer.ElapsedMilliseconds;
                _invalidated = false;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (App.IsDesigner)
                return;

            StopRendering();
            EndD3D();
        }

        private void PrepareAndCallRender()
        {
            if (_device == null)
            {
                return;
            }

            lock (_d2DRenderTarget)
            {
                _d2DRenderTarget.BeginDraw();
                Render(_d2DRenderTarget);
                _d2DRenderTarget.EndDraw();

                CalcFps();

                _device.ImmediateContext.Flush();
            }
        }

        private void StartD3D()
        {
            _device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);

            _surface = new Dx11ImageSource();
            _surface.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;

            Source = _surface;

            CreateAndBindTargets();
        }

        private void StartRendering()
        {
            if (_renderTimer.IsRunning)
            {
                return;
            }

            CompositionTarget.Rendering += OnRendering;

            _renderTimer.Start();
        }

        private void StopRendering()
        {
            if (!_renderTimer.IsRunning)
            {
                return;
            }

            CompositionTarget.Rendering -= OnRendering;

            _renderTimer.Stop();
        }

        public virtual void InvalidateSurface(Rectangle? area)
        {
            _invalidated = true;

            if(_surface == null)
                return;

            var whole = new Rectangle(0, 0, _d2DRenderTarget.PixelSize.Width, _d2DRenderTarget.PixelSize.Height);

            var rect = area ?? whole;
            var dpi = new Vector2(x: _d2DRenderTarget.DotsPerInch.Width, y: _d2DRenderTarget.DotsPerInch.Height) / new Vector2(96);

            rect.Top = (int)MathUtils.Clamp(0, whole.Height, rect.Top * dpi.Y);
            rect.Bottom = (int)MathUtils.Clamp(rect.Top, whole.Height, rect.Bottom * dpi.Y);
            rect.Left = (int)MathUtils.Clamp(0, whole.Width, rect.Left * dpi.X);
            rect.Right = (int)MathUtils.Clamp(rect.Left, whole.Width, rect.Right * dpi.X);

            if (rect.Width < 0) Debugger.Break();

            _dirty.Push(new Int32Rect(rect.X, rect.Y, rect.Width, rect.Height));
        }

        #endregion Methods
    }

    public class Dx11ImageSource : D3DImage, IDisposable
    {
        #region Fields

        private static int _activeClients;
        private static D3D9.Direct3DEx _context;
        private static D3D9.DeviceEx _device;

        private D3D9.Texture _renderTarget;

        #endregion Fields

        #region Constructors

        public Dx11ImageSource()
        {
            StartD3D();
            _activeClients++;
        }

        #endregion Constructors

        #region Methods

        public void Dispose()
        {
            SetRenderTarget(null);

            Disposer.SafeDispose(ref _renderTarget);

            _activeClients--;
            EndD3D();
        }

        public void InvalidateD3DImage(Int32Rect? rect = null)
        {
            if (_renderTarget != null)
            {
                lock (this)
                {
                    Lock();
                    AddDirtyRect(rect ?? new Int32Rect(0, 0, PixelWidth, PixelHeight));
                    Unlock();
                }
            }
        }

        public void SetRenderTarget(Texture2D target)
        {
            if (_renderTarget != null)
            {
                _renderTarget = null;

                base.Lock();
                base.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                base.Unlock();
            }

            if (target == null)
            {
                return;
            }

            var format = TranslateFormat(target);
            var handle = GetSharedHandle(target);

            if (!IsShareable(target))
            {
                throw new ArgumentException("Texture must be created with ResouceOptionFlags.Shared");
            }

            if (format == D3D9.Format.Unknown)
            {
                throw new ArgumentException("Texture format is not compatible with OpenSharedResouce");
            }

            if (handle == IntPtr.Zero)
            {
                throw new ArgumentException("Invalid handle");
            }

            _renderTarget = new D3D9.Texture(_device,
                target.Description.Width, target.Description.Height, 1,
                D3D9.Usage.RenderTarget, format, D3D9.Pool.Default, ref handle);

            using (var surface = _renderTarget.GetSurfaceLevel(0))
            {
                base.Lock();
                base.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
                base.Unlock();
            }
        }

        private static D3D9.PresentParameters GetPresentParameters()
        {
            var presentParams = new D3D9.PresentParameters
            {
                Windowed = true,
                SwapEffect = D3D9.SwapEffect.Discard,
                DeviceWindowHandle = NativeMethods.GetDesktopWindow(),
                PresentationInterval = D3D9.PresentInterval.Default
            };


            return presentParams;
        }

        private static bool IsShareable(Texture2D texture)
        {
            return (texture.Description.OptionFlags & ResourceOptionFlags.Shared) != 0;
        }

        private static void ResetD3D()
        {
            if (_activeClients == 0)
            {
                return;
            }

            var presentParams = GetPresentParameters();
            _device.ResetEx(ref presentParams);
        }

        private static D3D9.Format TranslateFormat(Texture2D texture)
        {
            switch (texture.Description.Format)
            {
                case SharpDX.DXGI.Format.R10G10B10A2_UNorm: return D3D9.Format.A2B10G10R10;
                case SharpDX.DXGI.Format.R16G16B16A16_Float: return D3D9.Format.A16B16G16R16F;
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm: return D3D9.Format.A8R8G8B8;
                default: return D3D9.Format.Unknown;
            }
        }

        private void EndD3D()
        {
            if (_activeClients != 0)
            {
                return;
            }

            Disposer.SafeDispose(ref _renderTarget);
            Disposer.SafeDispose(ref _device);
            Disposer.SafeDispose(ref _context);
        }

        private IntPtr GetSharedHandle(Texture2D texture)
        {
            using (var resource = texture.QueryInterface<SharpDX.DXGI.Resource>())
            {
                return resource.SharedHandle;
            }
        }

        private void StartD3D()
        {
            if (_activeClients != 0)
            {
                return;
            }

            var presentParams = GetPresentParameters();
            var createFlags = D3D9.CreateFlags.HardwareVertexProcessing | D3D9.CreateFlags.Multithreaded | D3D9.CreateFlags.FpuPreserve;

            _context = new D3D9.Direct3DEx();
            _device = new D3D9.DeviceEx(_context, 0, D3D9.DeviceType.Hardware, IntPtr.Zero, createFlags, presentParams);
        }

        #endregion Methods

        #region Classes

        private static class NativeMethods
        {
            #region Methods

            [DllImport("user32.dll", SetLastError = false)]
            public static extern IntPtr GetDesktopWindow();

            #endregion Methods
        }

        #endregion Classes
    }

    public enum RenderMode
    {
        Constant, Manual
    }

    internal static class Disposer
    {
        #region Methods

        public static void SafeDispose<T>(ref T resource) where T : class
        {
            if (resource == null)
            {
                return;
            }

            if (resource is IDisposable disposer)
            {
                try
                {
                    disposer.Dispose();
                }
                catch
                {
                }
            }

            resource = null;
        }

        #endregion Methods
    }
}