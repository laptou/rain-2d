using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Rain.Core;
using Rain.Core.Input;
using Rain.Native;
using Rain.Renderer.Direct2D;
using Rain.Utility;

using SharpDX.Direct3D;
using SharpDX.DXGI;

using DX = SharpDX;
using D3D = SharpDX.Direct3D11;
using D2D = SharpDX.Direct2D1;
using DW = SharpDX.DirectWrite;
using MouseButton = Rain.Core.Input.MouseButton;

namespace Rain.View.Control
{
    internal static class Disposer
    {
        public static void SafeDispose<T>(ref T resource) where T : class
        {
            if (resource == null)
                return;

            if (resource is IDisposable disposer)
                try
                {
                    disposer.Dispose();
                }
                catch { }

            resource = null;
        }
    }

    public abstract class D2DImage : HwndHost
    {
        protected D2DImage()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;

            if (App.IsDesigner) return;

            SizeChanged += OnSizeChanged;
            IsVisibleChanged += OnIsVisibleChanged;
            AllowDrop = true;
        }

        public D2D.Factory Direct2DFactory => _factory;

        public DW.Factory DirectWriteFactory => _dwFactory;

        public bool EnableAntialiasing
        {
            get => _target?.AntialiasMode == D2D.AntialiasMode.Aliased;
            set => _target.AntialiasMode =
                       value ? D2D.AntialiasMode.PerPrimitive : D2D.AntialiasMode.Aliased;
        }

        public RenderContext RenderContext { get; set; }

        public event EventHandler RenderTargetCreated;

        protected abstract void OnInput(IInputEvent inputEvent);

        protected abstract void OnRender(RenderContext renderContext);

        public virtual void InvalidateSurface()
        {
            _invalidated = true;

            

            WindowHelper.RedrawWindow(_host, IntPtr.Zero, IntPtr.Zero, 0b0001);
        }

        protected virtual IntPtr OnMessage(
            IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WindowMessage.Paint:
                    if (_target == null) goto default;

                    WindowHelper.BeginPaint(hWnd, out var paintStruct);

                    try
                    {
                        if (_invalidated)
                        {
                            _invalidated = false;
                            RenderContext.Begin(null);
                            OnRender(RenderContext);
                            RenderContext.End();
                            _swapChain.Present(0, PresentFlags.None, default);
                        }
                    }
                    catch (DX.SharpDXException ex) when (ex.Descriptor ==
                                                         D2D.ResultCode.RecreateTarget)
                    {
                        Initialize();
                    }

                    WindowHelper.EndPaint(hWnd, ref paintStruct);

                    _invalidated = false;

                    break;
                case WindowMessage.MouseWheelHorizontal:
                case WindowMessage.MouseWheel:
                {
                    var delta = NativeHelper.HighWord(wParam);

                    var pos = NativeHelper.GetCoordinates(lParam, _dpi, hWnd);

                    var scrollEvt = new ScrollEvent(delta,
                                                    pos,
                                                    msg == WindowMessage.MouseWheel
                                                        ? ScrollDirection.Vertical
                                                        : ScrollDirection.Horizontal,
                                                    KeyboardHelper.GetModifierState(wParam));

                    lock (_events)
                    {
                        _events.Enqueue(scrollEvt);
                    }

                    _eventFlag.Set();

                    break;
                }

                case WindowMessage.MouseMove:
                {
                    WindowHelper.SetFocus(hWnd);

                    var pos = NativeHelper.GetCoordinates(lParam, _dpi);

                    lock (_events)
                    {
                        _events.Enqueue(new PointerEvent(pos,
                                                         _lastMousePos - pos,
                                                         KeyboardHelper.GetModifierState(wParam)));
                    }

                    _eventFlag.Set();
                    _lastMousePos = pos;

                    break;
                }

                case WindowMessage.LeftButtonDown:
                {
                    WindowHelper.SetFocus(hWnd);

                    var pos = NativeHelper.GetCoordinates(lParam, _dpi);

                    lock (_events)
                    {
                        _events.Enqueue(new ClickEvent(pos,
                                                       MouseButton.Left,
                                                       ClickType.Down,
                                                       KeyboardHelper.GetModifierState(wParam)));
                    }

                    _eventFlag.Set();
                    _lastMousePos = pos;

                    break;
                }
                case WindowMessage.LeftButtonUp:
                {
                    WindowHelper.SetFocus(hWnd);

                    var pos = NativeHelper.GetCoordinates(lParam, _dpi);

                    lock (_events)
                    {
                        _events.Enqueue(new ClickEvent(pos,
                                                       MouseButton.Left,
                                                       ClickType.Up,
                                                       KeyboardHelper.GetModifierState(wParam)));
                    }

                    _eventFlag.Set();
                    _lastMousePos = pos;

                    break;
                }
                case WindowMessage.LeftButtonDoubleClick:
                {
                    WindowHelper.SetFocus(hWnd);

                    var pos = NativeHelper.GetCoordinates(lParam, _dpi);

                    lock (_events)
                    {
                        _events.Enqueue(new ClickEvent(pos,
                                                       MouseButton.Left,
                                                       ClickType.Double,
                                                       KeyboardHelper.GetModifierState(wParam)));
                    }

                    _eventFlag.Set();
                    _lastMousePos = pos;

                    break;
                }
                case WindowMessage.MouseLeave:

                    // lose focus when mouse isn't over this
                    WindowHelper.SetFocus(_parent);

                    break;
                case WindowMessage.KeyDown:
                case WindowMessage.SysKeyDown:
                    var repeat = (int) lParam & (1 << 30);
                    var key = KeyInterop.KeyFromVirtualKey((int) wParam);

                    if (repeat != 0) goto default;

                    lock (_events)
                    {
                        _events.Enqueue(
                            new KeyboardEvent((int) key, true, KeyboardHelper.GetModifierState()));
                    }

                    _eventFlag.Set();

                    // since the messages are processed asynchronously, 
                    // return 1, meaning it was not handled yet
                    return (IntPtr) 1;
                case WindowMessage.KeyUp:
                case WindowMessage.SysKeyUp:
                    key = KeyInterop.KeyFromVirtualKey((int) wParam);

                    lock (_events)
                    {
                        _events.Enqueue(
                            new KeyboardEvent((int) key, false, KeyboardHelper.GetModifierState()));
                    }

                    _eventFlag.Set();

                    return (IntPtr) 1;
                case WindowMessage.Char:
                case WindowMessage.UniChar:
                case WindowMessage.ImeChar:
                    var str = char.ConvertFromUtf32((int) wParam);

                    // ignore control characters such as backspace
                    if (str.Length == 1 &&
                        char.IsControl(str[0]))
                        break;

                    lock (_events)
                    {
                        _events.Enqueue(new TextEvent(str, KeyboardHelper.GetModifierState()));
                    }

                    _eventFlag.Set();

                    break;

                // here we call OnInput() directly instead of using the queue
                // because any delay in responding to focus events causes problems
                case WindowMessage.SetFocus:
                    OnInput(new FocusEvent(true, KeyboardHelper.GetModifierState()));
                    goto default;
                case WindowMessage.KillFocus:
                    OnInput(new FocusEvent(false, KeyboardHelper.GetModifierState()));
                    goto default;
                case WindowMessage.Size:
                    _dpi = WindowHelper.GetDpiForWindow(hWnd);

                    if (_swapChain != null)
                    {
                        RenderContext.Dispose();

                        _swapChain.ResizeBuffers(0, 0, 0, Format.Unknown, SwapChainFlags.None);

                        var targetProps = new D2D.RenderTargetProperties(
                            D2D.RenderTargetType.Hardware,
                            new D2D.PixelFormat(Format.B8G8R8A8_UNorm, D2D.AlphaMode.Ignore),
                            _dpi,
                            _dpi,
                            D2D.RenderTargetUsage.None,
                            D2D.FeatureLevel.Level_10);

                        using (var surf = _swapChain.GetBackBuffer<Surface>(0))
                            _target = new D2D.RenderTarget(_factory, surf, targetProps);

                        RenderContext = new Direct2DRenderContext(_target);
                    }

                    InvalidateSurface();
                    goto default;
                case WindowMessage.DropFiles:
                    var numFiles = DragHelper.DragQueryFile(wParam, 0xFFFFFFFF, null, 0);
                    var files = new List<string>(numFiles);

                    for (var i = 0u; i < numFiles; i++)
                    {
                        var sb = new StringBuilder(2048);
                        DragHelper.DragQueryFile(wParam, i, sb, 2048);
                        files.Add(sb.ToString());
                    }

                    DragHelper.DragQueryPoint(wParam, out var pt);

                    OnInput(new DropEvent(files, new Vector2(pt.x, pt.y) / _dpi * 96f));

                    DragHelper.DragAcceptFiles(hWnd, true);

                    break;
                default:

                    return WindowHelper.DefWindowProc(hWnd, msg, wParam, lParam);
            }

            return IntPtr.Zero;
        }

        protected override Size ArrangeOverride(Size arrangeSize) { return arrangeSize; }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            var wndClass = new WndClass
            {
                style = 3, // CS_VREDRAW | CS_HREDRAW
                lpszClassName = "d2d",
                hInstance = Marshal.GetHINSTANCE(GetType().Module),
                lpfnWndProc = _proc = OnMessage
            };

            var cls = WindowHelper.RegisterClass(ref wndClass);

            if (cls == 0)
            {
                var code = Marshal.GetLastWin32Error();

                throw new Win32Exception(code);
            }

            _parent = hwndParent.Handle;
            _host = WindowHelper.CreateWindowEx(WindowStylesEx.AcceptFiles,
                                                cls,
                                                "",
                                                WindowStyles.Child | WindowStyles.Visible,
                                                0,
                                                0,
                                                (int) ActualWidth + 1,
                                                (int) ActualHeight + 1,
                                                hwndParent.Handle,
                                                (IntPtr) 1,
                                                IntPtr.Zero,
                                                0);

            if (_host == IntPtr.Zero)
            {
                var code = Marshal.GetLastWin32Error();

                throw new Win32Exception(code);
            }

            WindowHelper.UpdateWindow(_host);

            Initialize();

            return new HandleRef(this, _host);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            Disposer.SafeDispose(ref _factory);
            Disposer.SafeDispose(ref _dwFactory);
            Disposer.SafeDispose(ref _target);
            Disposer.SafeDispose(ref _swapChain);
            Disposer.SafeDispose(ref _d3dDevice);
            Disposer.SafeDispose(ref _dxgiDevice);

            WindowHelper.DestroyWindow(hwnd.Handle);
            _proc = null;
        }

        protected override Size MeasureOverride(Size constraint) { return constraint; }

        private void EventLoop()
        {
            while (_eventLoop)
            {
                while (_events.Count > 0)
                {
                    IInputEvent evt;

                    lock (_events)
                    {
                        evt = _events.Dequeue();
                    }

                    // discard old events
                    if (Time.Now - evt.Timestamp > 500) continue;

                    OnInput(evt);
                }

                _eventFlag.Reset();

                _eventFlag.WaitOne(5000);
            }
        }

        private void Initialize()
        {
            Disposer.SafeDispose(ref _factory);
            Disposer.SafeDispose(ref _swapChain);
            Disposer.SafeDispose(ref _target);

            #if DEBUG
            _factory =
                new D2D.Factory1(D2D.FactoryType.MultiThreaded, D2D.DebugLevel.Information);
            #else
            _d2dFactory = new D2D.Factory(D2D.FactoryType.MultiThreaded, D2D.DebugLevel.None);
                                                                        #endif

            if(_dwFactory == null)
            _dwFactory = new DW.Factory(DW.FactoryType.Shared);

            if(_d3dDevice == null)
            _d3dDevice = new D3D.Device(DriverType.Hardware,
                                           D3D.DeviceCreationFlags.BgraSupport |
                                           D3D.DeviceCreationFlags.Debug);
            if(_dxgiDevice == null)
                _dxgiDevice = _d3dDevice.QueryInterface<Device>();

            var swapChainDesc = new SwapChainDescription1
            {
                Width = 0,
                Height = 0,
                Format = Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = new SampleDescription(1, 0), // no multisampling,
                BufferCount = 2, // use double buffering to enable flip,
                Usage = Usage.RenderTargetOutput,
#warning does not work in win7
                Scaling = Scaling.None,
                SwapEffect = SwapEffect.FlipSequential, // TODO: find out what this means
                AlphaMode = AlphaMode.Ignore
            };

            var dxgiAdapter = _dxgiDevice.Adapter;
            var dxgiFactory = dxgiAdapter.GetParent<Factory2>();
            var swapChain = new SwapChain1(dxgiFactory, _dxgiDevice, _host, ref swapChainDesc);

            var dpi = WindowHelper.GetDpiForWindow(_host);

            var targetProps = new D2D.RenderTargetProperties(
                D2D.RenderTargetType.Hardware,
                new D2D.PixelFormat(Format.B8G8R8A8_UNorm, D2D.AlphaMode.Ignore),
                dpi,
                dpi,
                D2D.RenderTargetUsage.None,
                D2D.FeatureLevel.Level_10);
            
            using (var surf = swapChain.GetBackBuffer<Surface>(0))
                _target = new D2D.RenderTarget(_factory, surf, targetProps);

            _swapChain = swapChain;

            RenderContext = new Direct2DRenderContext(_target);

            RenderTargetCreated?.Invoke(this, null);
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                _eventLoop = true;

                if (_evtThread == null)
                {
                    _evtThread = new Thread(EventLoop) {Priority = ThreadPriority.AboveNormal};
                    _evtThread.Start();
                }
            }
            else
            {
                _eventLoop = false;
                _evtThread = null;
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Initialize();
        }

        #region Event Loop

        private readonly AutoResetEvent     _eventFlag = new AutoResetEvent(false);
        private readonly Queue<IInputEvent> _events    = new Queue<IInputEvent>();
        private          bool               _eventLoop;
        private          Thread             _evtThread;
        private          Vector2            _lastMousePos;

        #endregion


        #region Win32

        private IntPtr _host;
        private IntPtr _parent;

        // ReSharper disable once NotAccessedField.Local
        private WndProc _proc;
        private float   _dpi;

        #endregion

        #region Direct2D

        private bool              _invalidated = true;
        private D2D.Factory1      _factory;
        private D2D.Bitmap1       _targetBmp;
        private DW.Factory        _dwFactory;
        private D2D.RenderTarget _target;
        private SwapChain1        _swapChain;
        private D3D.Device _d3dDevice;
        private Device _dxgiDevice;

        #endregion
    }
}