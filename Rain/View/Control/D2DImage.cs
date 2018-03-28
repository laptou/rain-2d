using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using Rain.Annotations;
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
    public abstract class D2DImage : HwndHost, INotifyPropertyChanged
    {
        private readonly float[]   _frames    = new float[10];
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private          long      _frame;


        private float _frameTime;

        protected D2DImage()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
        }

        public D2D.Factory Direct2DFactory => _factory;

        public DW.Factory DirectWriteFactory => _dwFactory;

        public bool EnableAntialiasing
        {
            get => _target?.AntialiasMode == D2D.AntialiasMode.Aliased;
            set => _target.AntialiasMode =
                       value ? D2D.AntialiasMode.PerPrimitive : D2D.AntialiasMode.Aliased;
        }

        public float FrameTime
        {
            get => _frameTime;
            set
            {
                if (value.Equals(_frameTime)) return;

                _frameTime = value;
                OnPropertyChanged();
            }
        }

        public RenderContext RenderContext { get; set; }

        public event EventHandler RenderTargetCreated;

        protected abstract void OnInput(IInputEvent inputEvent);

        protected abstract void OnRender(RenderContext renderContext);

        public virtual void InvalidateSurface()
        {
            _invalidated = true;

            WindowHelper.RedrawWindow(_hwnd, IntPtr.Zero, IntPtr.Zero, 0b0001);
        }

        protected virtual IntPtr OnMessage(
            IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            void PushEvent(IInputEvent evt)
            {
                lock (_events)
                {
                    _events.Enqueue(evt);
                }

                _eventFlag.Set();
            }

            void OnMouseMove()
            {
                WindowHelper.SetFocus(hWnd);

                var pos = NativeHelper.GetCoordinates(lParam, _dpi);

                PushEvent(new PointerEvent(pos,
                                           _lastMousePos - pos,
                                           KeyboardHelper.GetModifierState(wParam)));

                _lastMousePos = pos;
            }

            void OnMouseWheel()
            {
                var delta = NativeHelper.HighWord(wParam);

                var pos = NativeHelper.GetCoordinates(lParam, _dpi, hWnd);

                PushEvent(new ScrollEvent(delta,
                                          pos,
                                          msg == WindowMessage.MouseWheel
                                              ? ScrollDirection.Vertical
                                              : ScrollDirection.Horizontal,
                                          KeyboardHelper.GetModifierState(wParam)));
            }

            void OnLeftMouseButtonDown()
            {
                WindowHelper.SetFocus(hWnd);

                var pos = NativeHelper.GetCoordinates(lParam, _dpi);

                PushEvent(new ClickEvent(pos,
                                         MouseButton.Left,
                                         ClickType.Down,
                                         KeyboardHelper.GetModifierState(wParam)));

                _lastMousePos = pos;
            }

            void OnLeftMouseButtonUp()
            {
                WindowHelper.SetFocus(hWnd);

                var pos = NativeHelper.GetCoordinates(lParam, _dpi);

                PushEvent(new ClickEvent(pos,
                                         MouseButton.Left,
                                         ClickType.Up,
                                         KeyboardHelper.GetModifierState(wParam)));

                _lastMousePos = pos;
            }

            void OnLeftMouseButtonDoubleClick()
            {
                WindowHelper.SetFocus(hWnd);

                var pos = NativeHelper.GetCoordinates(lParam, _dpi);

                PushEvent(new ClickEvent(pos,
                                         MouseButton.Left,
                                         ClickType.Double,
                                         KeyboardHelper.GetModifierState(wParam)));

                _lastMousePos = pos;
            }
            
            switch (msg)
            {
                case WindowMessage.Paint:
                    Render();
                    WindowHelper.ValidateRect(_hwnd, IntPtr.Zero);

                    break;

                case WindowMessage.EraseBackground:

                    // return 1, meaning that the background was erased
                    return (IntPtr) 1;
                case WindowMessage.MouseWheelHorizontal:
                case WindowMessage.MouseWheel:
                    OnMouseWheel();

                    break;

                case WindowMessage.MouseMove:
                    OnMouseMove();

                    break;

                case WindowMessage.LeftButtonDown:
                    OnLeftMouseButtonDown();

                    break;

                case WindowMessage.LeftButtonUp:
                    OnLeftMouseButtonUp();

                    break;

                case WindowMessage.LeftButtonDoubleClick:
                    OnLeftMouseButtonDoubleClick();

                    break;

                case WindowMessage.MouseLeave:

                    // lose focus when mouse isn't over this
                    WindowHelper.SetFocus(_parent);

                    break;

                case WindowMessage.KeyDown:
                case WindowMessage.SysKeyDown:
                    var repeat = (int) lParam & (1 << 30);
                    var key = KeyInterop.KeyFromVirtualKey((int) wParam);
                    var state = KeyboardHelper.GetModifierState();

                    PushEvent(new KeyboardEvent((int) key,
                                                true,
                                                repeat != 0,
                                                state));

                    // since the messages are processed asynchronously, 
                    // return 1, meaning it was not handled yet
                    return (IntPtr) 1;
                case WindowMessage.KeyUp:
                case WindowMessage.SysKeyUp:
                    key = KeyInterop.KeyFromVirtualKey((int) wParam);

                    PushEvent(new KeyboardEvent((int) key,
                                                false,
                                                false,
                                                KeyboardHelper.GetModifierState()));

                    return (IntPtr) 1;
                case WindowMessage.Char:
                case WindowMessage.UniChar:
                case WindowMessage.ImeChar:
                    var str = char.ConvertFromUtf32((int) wParam);

                    // ignore control characters such as backspace
                    if (str.Length == 1 &&
                        char.IsControl(str[0]))
                        break;

                    PushEvent(new TextEvent(str, KeyboardHelper.GetModifierState()));

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

                        _swapChain.ResizeBuffers(0,
                                                 0,
                                                 0,
                                                 Format.Unknown,
                                                 SwapChainFlags.FrameLatencyWaitAbleObject);

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

                case WindowMessage.Close:
                    WindowHelper.DestroyWindow(_hwnd);

                    break;

                case WindowMessage.Destroy:
                    WindowHelper.PostQuitMessage(0);

                    break;

                default:

                    return WindowHelper.DefWindowProc(hWnd, msg, wParam, lParam);
            }

            return IntPtr.Zero;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
            _hwnd = WindowHelper.CreateWindowEx(
                WindowStylesEx.AcceptFiles | WindowStylesEx.Composited,
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

            if (_hwnd == IntPtr.Zero) NativeHelper.CheckError();

            WindowHelper.UpdateWindow(_hwnd);

            Initialize();

            _loop = true;

            _evtThread = new Thread(EventLoop) {Priority = ThreadPriority.AboveNormal};
            _evtThread.Start();

            return new HandleRef(this, _hwnd);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            _loop = false;

            Disposer.SafeDispose(ref _factory);
            Disposer.SafeDispose(ref _dwFactory);
            Disposer.SafeDispose(ref _target);
            Disposer.SafeDispose(ref _swapChain);
            Disposer.SafeDispose(ref _d3dDevice);
            Disposer.SafeDispose(ref _dxgiDevice);

            _proc = null;
        }

        protected override Size MeasureOverride(Size constraint) { return constraint; }

        private void EventLoop()
        {
            bool TryGetEvent(out IInputEvent evt)
            {
                lock (_events)
                {
                    if (_events.Count > 0)
                    {
                        evt = _events.Dequeue();

                        return true;
                    }

                    evt = null;

                    return false;
                }
            }

            var threshold = Stopwatch.Frequency / 60;
            var factor = 1d / Stopwatch.Frequency * 1000;

            while (_loop)
            {
                while (TryGetEvent(out var evt))
                {
                    var latency = Stopwatch.GetTimestamp() - evt.Timestamp;

                    // discard old events
                    if (latency > threshold)
                    {
                        #if DEBUG
                        Trace.WriteLine($"Input event dropped: {evt}, " +
                                        $"latency {latency * factor}ms");
                        #endif
                        continue;
                    }

                    OnInput(evt);

                    var time = Stopwatch.GetTimestamp() - evt.Timestamp;

                    #if DEBUG
                    if (time > threshold)
                        Trace.WriteLine($"Input event took too long: {evt}, " +
                                        $"latency {time * factor}ms");
                    #endif
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
            _factory = new D2D.Factory1(D2D.FactoryType.MultiThreaded, D2D.DebugLevel.Information);
            #else
            _factory = new D2D.Factory1(D2D.FactoryType.MultiThreaded, D2D.DebugLevel.None);
                                                #endif

            if (_dwFactory == null)
                _dwFactory = new DW.Factory(DW.FactoryType.Shared);

            if (_d3dDevice == null)
                _d3dDevice = new D3D.Device(DriverType.Hardware,
                                            D3D.DeviceCreationFlags.BgraSupport |
                                            D3D.DeviceCreationFlags.Debug);
            if (_dxgiDevice == null)
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
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential, // TODO: find out what this means
                AlphaMode = AlphaMode.Ignore,
                Flags = SwapChainFlags.FrameLatencyWaitAbleObject
            };

            if (VersionHelper.RequireVersion(6, 3, 0, 0))
                swapChainDesc.Scaling = Scaling.None;

            var dxgiAdapter = _dxgiDevice.Adapter;
            var dxgiFactory = dxgiAdapter.GetParent<Factory2>();
            var swapChain = new SwapChain1(dxgiFactory, _dxgiDevice, _hwnd, ref swapChainDesc);

            var dpi = WindowHelper.GetDpiForWindow(_hwnd);

            var targetProps = new D2D.RenderTargetProperties(
                D2D.RenderTargetType.Hardware,
                new D2D.PixelFormat(Format.B8G8R8A8_UNorm, D2D.AlphaMode.Ignore),
                dpi,
                dpi,
                D2D.RenderTargetUsage.None,
                D2D.FeatureLevel.Level_10);

            using (var surf = swapChain.GetBackBuffer<Surface>(0))
            {
                _target = new D2D.RenderTarget(_factory, surf, targetProps);
            }

            _swapChain = swapChain.QueryInterface<SwapChain2>();
            _swapChain.MaximumFrameLatency = 1;
            _frameLatencyWaitHandle = _swapChain.FrameLatencyWaitableObject;

            RenderContext = new Direct2DRenderContext(_target);

            RenderTargetCreated?.Invoke(this, null);
        }

        private void Render()
        {
            if (_target == null ||
                !_invalidated) return;

            try
            {
                _invalidated = false;

                _stopwatch.Restart();

                NativeHelper.WaitForSingleObjectEx(_frameLatencyWaitHandle, 1000, true);

                RenderContext.Begin(null);
                OnRender(RenderContext);
                RenderContext.End();

                _swapChain.Present(1, PresentFlags.None, default);

                _stopwatch.Stop();

                _frames[_frame++ % 10] = (float) _stopwatch.Elapsed.TotalMilliseconds;

                if (_frame % 10 == 0)
                    FrameTime = _frames.Average();
            }
            catch (DX.SharpDXException ex) when (ex.Descriptor == D2D.ResultCode.RecreateTarget)
            {
                Initialize();
                _invalidated = true;
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Event Loop

        private readonly AutoResetEvent     _eventFlag = new AutoResetEvent(false);
        private readonly Queue<IInputEvent> _events    = new Queue<IInputEvent>();
        private          bool               _loop;
        private          Thread             _evtThread;
        private          Vector2            _lastMousePos;

        #endregion


        #region Win32

        private IntPtr _hwnd;
        private IntPtr _parent;

        // ReSharper disable once NotAccessedField.Local
        private WndProc _proc;
        private float   _dpi;

        #endregion

        #region Direct2D

        private bool             _invalidated = true;
        private D2D.Factory1     _factory;
        private DW.Factory       _dwFactory;
        private D2D.RenderTarget _target;
        private SwapChain2       _swapChain;
        private D3D.Device       _d3dDevice;
        private Device           _dxgiDevice;
        private IntPtr           _frameLatencyWaitHandle;

        #endregion
    }
}