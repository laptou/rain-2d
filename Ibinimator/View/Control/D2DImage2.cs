using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Ibinimator.Core;
using Ibinimator.Native;
using Ibinimator.Renderer.Direct2D;
using Ibinimator.Service;

using DX = SharpDX;
using D2D = SharpDX.Direct2D1;
using DW = SharpDX.DirectWrite;
using Format = SharpDX.DXGI.Format;

namespace Ibinimator.View.Control
{
    public abstract class D2DImage2 : HwndHost
    {
        // Event loop stuff
        private readonly AutoResetEvent _eventFlag = new AutoResetEvent(false);

        private readonly Queue<InputEvent> _events = new Queue<InputEvent>();

        private D2D.Factory _d2dFactory;
        private DW.Factory  _dwFactory;
        private bool        _eventLoop;
        private Thread      _evtThread;

        // Win32 stuff
        private IntPtr _host;
        private bool   _invalidated = true;

        private IntPtr _parent;

        // ReSharper disable once NotAccessedField.Local
        private WndProc _proc;

        // Direct2D stuff
        private D2D.WindowRenderTarget _renderTarget;

        protected D2DImage2()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;

            if (App.IsDesigner) return;

            SizeChanged += OnSizeChanged;
            IsVisibleChanged += OnIsVisibleChanged;
        }

        public D2D.Factory Direct2DFactory => _d2dFactory;

        public DW.Factory DirectWriteFactory => _dwFactory;

        public bool EnableAntialiasing
        {
            get => _renderTarget?.AntialiasMode == D2D.AntialiasMode.Aliased;
            set => _renderTarget.AntialiasMode =
                       value ? D2D.AntialiasMode.PerPrimitive : D2D.AntialiasMode.Aliased;
        }

        public RenderContext RenderContext { get; set; }

        public event EventHandler RenderTargetCreated;

        protected abstract void HandleInput(InputEvent evt);

        protected abstract void Render(RenderContext renderContext);

        public virtual void InvalidateSurface()
        {
            _invalidated = true;
            NativeHelper.InvalidateRect(_host, IntPtr.Zero, false);
        }

        protected override Size ArrangeOverride(Size arrangeSize) { return arrangeSize; }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            var wndClass = new WndClass
            {
                style = 3, // CS_VREDRAW | CS_HREDRAW
                lpszClassName = "d2d",
                hInstance = Marshal.GetHINSTANCE(GetType().Module),
                lpfnWndProc = _proc = WndProc
            };

            var cls = NativeHelper.RegisterClass(ref wndClass);

            if (cls == 0)
            {
                var code = Marshal.GetLastWin32Error();

                throw new Win32Exception(code);
            }

            _parent = hwndParent.Handle;
            _host = NativeHelper.CreateWindowEx(
                0,
                cls, "",
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

            NativeHelper.UpdateWindow(_host);

            return new HandleRef(this, _host);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            NativeHelper.DestroyWindow(hwnd.Handle);
            _proc = null;
        }

        protected override Size MeasureOverride(Size constraint) { return constraint; }

        private void EventLoop()
        {
            while (_eventLoop)
            {
                while (_events.Count > 0)
                {
                    InputEvent evt;

                    lock (_events)
                    {
                        evt = _events.Dequeue();
                    }

                    // discard old events
                    if (Time.Now - evt.Time > 500) continue;

                    HandleInput(evt);
                }

                _eventFlag.Reset();

                _eventFlag.WaitOne(5000);
            }
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
                PresentOptions = D2D.PresentOptions.None
            };

            _renderTarget = new D2D.WindowRenderTarget(_d2dFactory, rtp, hrtp);

            RenderContext = new Direct2DRenderContext(_renderTarget);

            RenderTargetCreated?.Invoke(this, null);
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

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) { Initialize(); }

        private IntPtr WndProc(IntPtr hwnd, WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WindowMessage.Paint:
                    if (_renderTarget == null) goto default;

                    NativeHelper.BeginPaint(hwnd, out var paintStruct);

                    try
                    {
                        if (_invalidated && _renderTarget.CheckWindowState() == D2D.WindowState.None)
                        {
                            _invalidated = false;
                            RenderContext.Begin(null);
                            Render(RenderContext);
                            RenderContext.End();
                        }
                    }
                    catch (DX.SharpDXException ex) when
                        (ex.Descriptor == D2D.ResultCode.RecreateTarget)
                    {
                        Initialize();
                        RenderContext.Begin(null);
                        Render(RenderContext);
                        RenderContext.End();
                    }

                    NativeHelper.EndPaint(hwnd, ref paintStruct);

                    break;
                case WindowMessage.MouseWheelHorizontal:
                case WindowMessage.MouseWheel:
                    var delta = NativeHelper.HighWord(wParam);
                    var x = NativeHelper.LowWord(lParam) / _d2dFactory.DesktopDpi.Width * 96f;
                    var y = NativeHelper.HighWord(lParam) / _d2dFactory.DesktopDpi.Height * 96f;

                    var scrollEvt = new InputEvent(msg == WindowMessage.MouseWheel ?
                                                       InputEventType.ScrollVertical :
                                                       InputEventType.ScrollHorizontal,
                                                   delta, new Vector2(x, y),
                                                   NativeHelper.GetModifierState(wParam));

                    _events.Enqueue(scrollEvt);
                    _eventFlag.Set();

                    break;

                case WindowMessage.MouseMove:
                case WindowMessage.LeftButtonDown:
                case WindowMessage.LeftButtonUp:

                    // set focus to this if the mouse enters
                    NativeHelper.SetFocus(hwnd);

                    x = NativeHelper.LowWord(lParam) / _d2dFactory.DesktopDpi.Width * 96f;
                    y = NativeHelper.HighWord(lParam) / _d2dFactory.DesktopDpi.Height * 96f;

                    var clickType = InputEventType.MouseDoubleClick;

                    switch (msg)
                    {
                        case WindowMessage.MouseMove:
                            clickType = InputEventType.MouseMove;

                            break;
                        case WindowMessage.LeftButtonDown:
                            clickType = InputEventType.MouseDown;

                            break;
                        case WindowMessage.LeftButtonUp:
                            clickType = InputEventType.MouseUp;

                            break;
                    }

                    _events.Enqueue(
                        new InputEvent(clickType,
                                       new Vector2(x, y),
                                       NativeHelper.GetModifierState(wParam)));
                    _eventFlag.Set();

                    break;
                case WindowMessage.MouseLeave:

                    // lose focus when mouse isn't over this
                    NativeHelper.SetFocus(_parent);

                    break;
                case WindowMessage.KeyDown:
                case WindowMessage.SysKeyDown:
                    var repeat = (int) lParam & (1 << 30);
                    var key = KeyInterop.KeyFromVirtualKey((int) wParam);

                    if (repeat != 0) goto default;

                    _events.Enqueue(
                        new InputEvent(InputEventType.KeyDown,
                                       key, NativeHelper.GetModifierState()));
                    _eventFlag.Set();

                    // since the messages are processed asynchronously, 
                    // return 1, meaning it was not handled yet
                    return (IntPtr) 1;
                case WindowMessage.KeyUp:
                case WindowMessage.SysKeyUp:
                    key = KeyInterop.KeyFromVirtualKey((int) wParam);
                    _events.Enqueue(
                        new InputEvent(InputEventType.KeyUp,
                                       key, NativeHelper.GetModifierState()));
                    _eventFlag.Set();

                    return (IntPtr) 1;
                case WindowMessage.Char:
                case WindowMessage.UniChar:
                    var str = char.ConvertFromUtf32((int) wParam);

                    // ignore control characters such as backspace
                    if (str.Length == 1 && char.IsControl(str[0]))
                        break;

                    _events.Enqueue(new InputEvent(InputEventType.TextInput, str));
                    _eventFlag.Set();

                    break;
                case WindowMessage.SetFocus:
                    NativeHelper.CreateCaret(hwnd, IntPtr.Zero, 0, 0);

                    break;
                case WindowMessage.KillFocus:
                    NativeHelper.DestroyCaret();

                    break;
                case WindowMessage.Size:
                    InvalidateSurface();
                    goto default;
                default:

                    return NativeHelper.DefWindowProc(hwnd, msg, wParam, lParam);
            }

            return IntPtr.Zero;
        }
    }
}