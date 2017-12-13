using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ibinimator.Renderer.Direct2D;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Native;
using Ibinimator.Service;
using DX = SharpDX;
using D2D = SharpDX.Direct2D1;
using DW = SharpDX.DirectWrite;
using Format = SharpDX.DXGI.Format;

namespace Ibinimator.View.Control
{
    public abstract class D2DImage2 : HwndHost
    {
        // Direct2D stuff
        private D2D.RenderTarget _renderTarget;

        private D2D.Factory _d2dFactory;
        private DW.Factory _dwFactory;
        private bool _invalidated;

        // Windows stuff
        private IntPtr _host;

        private WndProc _proc;

        // Event loop stuff
        private readonly AutoResetEvent _eventFlag = new AutoResetEvent(false);

        private readonly Queue<InputEvent> _events = new Queue<InputEvent>();
        private bool _eventLoop;
        private Thread _evtThread;

        protected D2DImage2()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;

            if (App.IsDesigner) return;

            SizeChanged += OnSizeChanged;
            IsVisibleChanged += OnIsVisibleChanged;
        }

        private void OnIsVisibleChanged(
            object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                _eventLoop = true;

                if (_evtThread == null)
                {
                    _evtThread = new Thread(EventLoop);
                    _evtThread.Start();
                }
            }
            else
            {
                _eventLoop = false;
                _evtThread = null;
            }
        }

        private void EventLoop()
        {
            while (_eventLoop)
            {
                while (_events.Count > 0)
                {
                    InputEvent evt;

                    lock (_events)
                        evt = _events.Dequeue();

                    // discard old events
                    if (Time.Now - evt.Time > 500) continue;

                    HandleInput(evt);
                }

                _eventFlag.Reset();

                _eventFlag.WaitOne(5000);
            }
        }

        public event EventHandler RenderTargetCreated;
        protected abstract void HandleInput(InputEvent evt);

        protected override Size ArrangeOverride(Size arrangeSize) { return arrangeSize; }

        protected override Size MeasureOverride(Size constraint) { return constraint; }

        public RenderContext RenderContext { get; set; }

        protected abstract void Render(RenderContext renderContext);

        public virtual void InvalidateSurface() { _invalidated = true; }

        public D2D.Factory Direct2DFactory => _d2dFactory;

        public DW.Factory DirectWriteFactory => _dwFactory;

        public bool EnableAntialiasing
        {
            get => _renderTarget?.AntialiasMode == D2D.AntialiasMode.Aliased;
            set => _renderTarget.AntialiasMode =
                value ? D2D.AntialiasMode.PerPrimitive : D2D.AntialiasMode.Aliased;
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            var wndClass = new WndClass
            {
                lpszClassName = "d2d",
                hInstance = Marshal.GetHINSTANCE(GetType().Module),
                lpfnWndProc = _proc = WndProc
            };

            var cls = WndClass.RegisterClass(ref wndClass);

            _host = WndClass.CreateWindowEx(
                0, cls, "",
                WindowStyles.Child |
                WindowStyles.Visible,
                0, 0,
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

            Initialize();

            return new HandleRef(this, _host);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            WndClass.DestroyWindow(hwnd.Handle);
            _proc = null;
        }

        private void Initialize()
        {
            Disposer.SafeDispose(ref _d2dFactory);
            Disposer.SafeDispose(ref _dwFactory);
            Disposer.SafeDispose(ref _renderTarget);

            _d2dFactory = new D2D.Factory(D2D.FactoryType.MultiThreaded);
            _dwFactory = new DW.Factory(DW.FactoryType.Shared);

            var width = (int) Math.Max(1, ActualWidth * _d2dFactory.DesktopDpi.Width / 96.0);
            var height = (int) Math.Max(1, ActualHeight * _d2dFactory.DesktopDpi.Height / 96.0);

            var rtp = new D2D.RenderTargetProperties(
                    new D2D.PixelFormat(Format.Unknown,
                                        D2D.AlphaMode.Premultiplied))
                {
                    DpiX = _d2dFactory.DesktopDpi.Width,
                    DpiY = _d2dFactory.DesktopDpi.Height,
                    Type = D2D.RenderTargetType.Hardware
                };

            var hrtp = new D2D.HwndRenderTargetProperties
            {
                Hwnd = _host,
                PixelSize = new DX.Size2(width, height),
                PresentOptions = D2D.PresentOptions.Immediately
            };

            _renderTarget = new D2D.WindowRenderTarget(_d2dFactory, rtp, hrtp);

            RenderContext = new Direct2DRenderContext(_renderTarget);

            RenderTargetCreated?.Invoke(this, null);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) { Initialize(); }

        private IntPtr WndProc(IntPtr hwnd, WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WindowMessage.Paint:
                    if (_renderTarget == null) return IntPtr.Zero;

                    try
                    {
                        if (_invalidated)
                        {
                            _invalidated = false;
                            _renderTarget.BeginDraw();
                            Render(RenderContext);
                            _renderTarget.EndDraw();
                        }
                    }
                    catch (DX.SharpDXException ex) when
                        (ex.Descriptor == D2D.ResultCode.RecreateTarget)
                    {
                        Initialize();
                    }

                    return IntPtr.Zero;
                case WindowMessage.MouseWheelHorizontal:
                case WindowMessage.MouseWheel:
                    var delta = NativeHelper.HighWord(wParam);
                    var x = NativeHelper.LowWord(lParam);
                    var y = NativeHelper.HighWord(lParam);

                    _events.Enqueue(
                        new InputEvent(msg == WindowMessage.MouseWheel ?
                                           InputEventType.ScrollVertical :
                                           InputEventType.ScrollHorizontal,
                                       delta, new Vector2(x, y)));
                    return IntPtr.Zero;

                case WindowMessage.LeftButtonDoubleClick:
                case WindowMessage.LeftButtonDown:
                case WindowMessage.LeftButtonUp:
                    x = NativeHelper.LowWord(lParam);
                    y = NativeHelper.HighWord(lParam);

                    var clickType = InputEventType.MouseDoubleClick;

                    switch (msg)
                    {
                        case WindowMessage.LeftButtonDown:
                            clickType = InputEventType.MouseDown;
                            break;
                        case WindowMessage.LeftButtonUp:
                            clickType = InputEventType.MouseUp;
                            break;
                    }

                    var btns =
                        (left: Mouse.LeftButton == MouseButtonState.Pressed,
                        middle: Mouse.MiddleButton == MouseButtonState.Pressed,
                        right: Mouse.RightButton == MouseButtonState.Pressed);

                    _events.Enqueue(
                        new InputEvent(clickType,
                                       btns.left, btns.middle, btns.right,
                                       new Vector2(x, y)));
                    return IntPtr.Zero;
                default:
                    return WndClass.DefWindowProc(hwnd, msg, wParam, lParam);
            }
        }
    }
}