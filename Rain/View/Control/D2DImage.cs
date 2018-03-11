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

using Rain.Core;
using Rain.Core.Input;
using Rain.Native;
using Rain.Renderer.Direct2D;
using Rain.Utility;

using SharpDX.DXGI;

using DX = SharpDX;
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
                    if (_renderTarget == null) goto default;

                    WindowHelper.BeginPaint(hWnd, out var paintStruct);

                    try
                    {
                        if (_invalidated &&
                            _renderTarget.CheckWindowState() == D2D.WindowState.None)
                        {
                            _invalidated = false;
                            RenderContext.Begin(null);
                            OnRender(RenderContext);
                            RenderContext.End();
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

                    var pos = NativeHelper.GetCoordinates(lParam,
                                                          _dpi,
                                                          hWnd);

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
                    InvalidateSurface();
                    _dpi = WindowHelper.GetDpiForWindow(hWnd);
                    goto default;
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
            _host = WindowHelper.CreateWindowEx(0,
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

            Initialize();

            WindowHelper.UpdateWindow(_host);

            return new HandleRef(this, _host);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
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
            Disposer.SafeDispose(ref _d2dFactory);
            Disposer.SafeDispose(ref _dwFactory);
            Disposer.SafeDispose(ref _renderTarget);

            #if DEBUG
            _d2dFactory =
                new D2D.Factory(D2D.FactoryType.MultiThreaded, D2D.DebugLevel.Information);
            #else
            _d2dFactory = new D2D.Factory(D2D.FactoryType.MultiThreaded, D2D.DebugLevel.None);
            #endif

            _dwFactory = new DW.Factory(DW.FactoryType.Shared);

            var width = (int) Math.Max(1, ActualWidth * _dpi / 96.0);
            var height = (int) Math.Max(1, ActualHeight * _dpi / 96.0);

            var rtp = new D2D.RenderTargetProperties(
                    new D2D.PixelFormat(Format.Unknown, D2D.AlphaMode.Premultiplied))
                {
                    DpiX = _dpi,
                    DpiY = _dpi,
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

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                _eventLoop = true;

                if (_evtThread == null)
                {
                    _evtThread = new Thread(EventLoop);
                    _evtThread.Priority = ThreadPriority.AboveNormal;
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
        private float _dpi;

        #endregion

        #region Direct2D

        private bool                   _invalidated = true;
        private D2D.Factory            _d2dFactory;
        private DW.Factory             _dwFactory;
        private D2D.WindowRenderTarget _renderTarget;

        #endregion
    }
}