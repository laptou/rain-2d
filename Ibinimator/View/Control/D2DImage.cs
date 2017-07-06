using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using D2D = SharpDX.Direct2D1;
using D3D9 = SharpDX.Direct3D9;
using Screen = System.Windows.Forms.Screen;
using Ibinimator.Shared;
using System.Windows.Media;

namespace Ibinimator.View.Control
{
    public abstract class D2DImage : Image
    {
        #region Fields
        private static readonly DependencyPropertyKey FPSPropertyKey = DependencyProperty.RegisterReadOnly(
           "FPS",
           typeof(int),
           typeof(D2DImage),
           new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.None)
           );

        public static readonly DependencyProperty FPSProperty = FPSPropertyKey.DependencyProperty;
        

        private readonly Stopwatch renderTimer = new Stopwatch();
        private D2D.Factory d2DFactory;
        private D2D.RenderTarget d2DRenderTarget;
        private Device device;
        private int frameCount = 0;
        private Queue<int> frameCountHist = new Queue<int>();
        private int frameCountHistTotal = 0;
        private long lastFrameTime = 0;
        private long lastRenderTime = 0;
        private Texture2D renderTarget;
        private DX11ImageSource surface;

        #endregion Fields

        #region Constructors

        public D2DImage()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            Stretch = Stretch.Fill;
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        }

        #endregion Constructors

        #region Properties

        public int FPS
        {
            get { return (int)GetValue(FPSProperty); }
            protected set { SetValue(FPSPropertyKey, value); }
        }

        public bool EnableAntialiasing
        {
            get => d2DRenderTarget?.AntialiasMode == D2D.AntialiasMode.Aliased;
            set => d2DRenderTarget.AntialiasMode = value ? D2D.AntialiasMode.PerPrimitive : D2D.AntialiasMode.Aliased;
        }

        public DX11ImageSource Surface => surface;

        #endregion Properties

        public event EventHandler<D2D.RenderTarget> RenderTargetBound;

        #region Methods

        public abstract void Render(D2D.RenderTarget target);

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            CreateAndBindTargets();
            base.OnRenderSizeChanged(sizeInfo);
        }

        private void CalcFps()
        {
            frameCount++;
            if (renderTimer.ElapsedMilliseconds - lastFrameTime > 1000)
            {
                frameCountHist.Enqueue(frameCount);
                frameCountHistTotal += frameCount;
                if (frameCountHist.Count > 5)
                {
                    frameCountHistTotal -= frameCountHist.Dequeue();
                }

                FPS = frameCountHistTotal / frameCountHist.Count;

                frameCount = 0;
                lastFrameTime = renderTimer.ElapsedMilliseconds;
            }
        }

        private void CreateAndBindTargets()
        {
            this.surface.SetRenderTarget(null);

            Disposer.SafeDispose(ref d2DRenderTarget);
            Disposer.SafeDispose(ref d2DFactory);
            Disposer.SafeDispose(ref renderTarget);

            var pt = PointToScreen(new Point());
            Screen screen = Screen.FromPoint(new System.Drawing.Point((int)pt.X, (int)pt.Y));
            var dpi = screen.GetDpiForMonitor(DpiType.Effective);


            var width = Math.Max((int)(ActualWidth * dpi.x / 96f), 100);
            var height = Math.Max((int)(ActualHeight * dpi.y / 96f), 100);

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

            renderTarget = new Texture2D(device, renderDesc);

            var surface = renderTarget.QueryInterface<SharpDX.DXGI.Surface>();

            d2DFactory = new D2D.Factory();
            var rtp = new D2D.RenderTargetProperties(new D2D.PixelFormat(SharpDX.DXGI.Format.Unknown, D2D.AlphaMode.Premultiplied));
            d2DRenderTarget = new D2D.RenderTarget(d2DFactory, surface, rtp);

            d2DRenderTarget.DotsPerInch = new SharpDX.Size2F(dpi.x, dpi.y);

            this.surface.SetRenderTarget(renderTarget);

            RenderTargetBound?.Invoke(this, d2DRenderTarget);

            device.ImmediateContext.Rasterizer.SetViewport(0, 0, width, height, 0.0f, 1.0f);
        }

        private void EndD3D()
        {
            surface.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
            Source = null;

            Disposer.SafeDispose(ref d2DRenderTarget);
            Disposer.SafeDispose(ref d2DFactory);
            Disposer.SafeDispose(ref surface);
            Disposer.SafeDispose(ref renderTarget);
            Disposer.SafeDispose(ref device);
        }

        private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (surface.IsFrontBufferAvailable)
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
            if (App.IsDesigner) return;

            StartD3D();
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (!renderTimer.IsRunning)
            {
                return;
            }

            PrepareAndCallRender();
            surface.InvalidateD3DImage();

            lastRenderTime = renderTimer.ElapsedMilliseconds;
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
            if (device == null)
            {
                return;
            }
            d2DRenderTarget.BeginDraw();
            Render(d2DRenderTarget);
            d2DRenderTarget.EndDraw();

            CalcFps();

            device.ImmediateContext.Flush();
        }

        private void StartD3D()
        {
            device = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);

            surface = new DX11ImageSource();
            surface.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;

            CreateAndBindTargets();

            Source = surface;
        }

        private void StartRendering()
        {
            if (renderTimer.IsRunning)
            {
                return;
            }

            System.Windows.Media.CompositionTarget.Rendering += OnRendering;
            renderTimer.Start();
        }

        private void StopRendering()
        {
            if (!renderTimer.IsRunning)
            {
                return;
            }

            System.Windows.Media.CompositionTarget.Rendering -= OnRendering;
            renderTimer.Stop();
        }

        #endregion Methods
    }

    public class DX11ImageSource : D3DImage, IDisposable
    {
        #region Fields

        private static int ActiveClients;
        private static D3D9.Direct3DEx context;
        private static D3D9.DeviceEx device;

        private D3D9.Texture renderTarget;

        #endregion Fields

        #region Constructors

        public DX11ImageSource()
        {
            StartD3D();
            ActiveClients++;
        }

        #endregion Constructors

        #region Methods

        public void Dispose()
        {
            SetRenderTarget(null);

            Disposer.SafeDispose(ref renderTarget);

            ActiveClients--;
            EndD3D();
        }

        public void InvalidateD3DImage()
        {
            if (renderTarget != null)
            {
                Lock();
                AddDirtyRect(new Int32Rect(0, 0, PixelWidth, PixelHeight));
                Unlock();
            }
        }

        public void SetRenderTarget(Texture2D target)
        {
            if (renderTarget != null)
            {
                renderTarget = null;

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

            renderTarget = new D3D9.Texture(device,
                target.Description.Width, target.Description.Height, 1,
                D3D9.Usage.RenderTarget, format, D3D9.Pool.Default, ref handle);

            using (var surface = renderTarget.GetSurfaceLevel(0))
            {
                base.Lock();
                base.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
                base.Unlock();
            }
        }

        private static D3D9.PresentParameters GetPresentParameters()
        {
            var presentParams = new D3D9.PresentParameters();

            presentParams.Windowed = true;
            presentParams.SwapEffect = D3D9.SwapEffect.Discard;
            presentParams.DeviceWindowHandle = NativeMethods.GetDesktopWindow();
            presentParams.PresentationInterval = D3D9.PresentInterval.Default;

            return presentParams;
        }

        private static bool IsShareable(Texture2D texture)
        {
            return (texture.Description.OptionFlags & ResourceOptionFlags.Shared) != 0;
        }

        private static void ResetD3D()
        {
            if (ActiveClients == 0)
            {
                return;
            }

            var presentParams = GetPresentParameters();
            device.ResetEx(ref presentParams);
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
            if (ActiveClients != 0)
            {
                return;
            }

            Disposer.SafeDispose(ref renderTarget);
            Disposer.SafeDispose(ref device);
            Disposer.SafeDispose(ref context);
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
            if (ActiveClients != 0)
            {
                return;
            }

            var presentParams = GetPresentParameters();
            var createFlags = D3D9.CreateFlags.HardwareVertexProcessing | D3D9.CreateFlags.Multithreaded | D3D9.CreateFlags.FpuPreserve;

            context = new D3D9.Direct3DEx();
            device = new D3D9.DeviceEx(context, 0, D3D9.DeviceType.Hardware, IntPtr.Zero, createFlags, presentParams);
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