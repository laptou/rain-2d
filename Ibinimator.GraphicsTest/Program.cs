using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;

using Ibinimator.Core;
using Ibinimator.Native;
using Ibinimator.Renderer.Direct2D;

using static Ibinimator.Native.NativeHelper;

using Color = Ibinimator.Core.Model.Color;
using D2D = SharpDX.Direct2D1;
using DX = SharpDX;
using Format = SharpDX.DXGI.Format;

namespace Ibinimator.GraphicsTest
{
    internal class Program
    {
        private AutoResetEvent         _evt = new AutoResetEvent(false);
        private WndProc                _proc;
        private D2D.WindowRenderTarget _renderTarget;
        private IntPtr _hwnd;
        public Direct2DRenderContext RenderContext { get; set; }

        private void FormOnPaint(object sender, PaintEventArgs e)
        {
            Paint();
            InvalidateRect(_hwnd, IntPtr.Zero, false);
        }

        private static void Main() { new Program().Run(); }

        private void Paint()
        {
            _renderTarget.BeginDraw();
            _renderTarget.Clear(new DX.Color());

            var time = DateTime.Now - DateTime.MinValue;

            /*var stops1 = new D2D.GradientStopCollection(
                _renderTarget,
                new[]
                {
                    new D2D.GradientStop {Color = new DX.Color(1f, 0, 0), Position = 0},
                    new D2D.GradientStop
                    {
                        Color = new DX.Color(0, 1f, 0),
                        Position = 0.5f + 0.4f * (float) Math.Sin(time.TotalSeconds)
                    },
                    new D2D.GradientStop {Color = new DX.Color(0, 0, 1f), Position = 1}
                });

            var native = new D2D.LinearGradientBrush(
                _renderTarget,
                new D2D.LinearGradientBrushProperties
                {
                    StartPoint = new DX.Vector2(0, 256),
                    EndPoint = new DX.Vector2(512, 256)
                },
                stops1);*/

            var brush = RenderContext.CreateBrush(
                new[]
                {
                    new GradientStop {Color = new Color(1f, 0, 0), Offset = 0},
                    new GradientStop
                    {
                        Color = new Color(0, 1f, 0),
                        Offset = 0.5f + 0.4f * (float) Math.Sin(time.TotalSeconds)
                    },
                    new GradientStop {Color = new Color(0, 0, 1f), Offset = 1}
                }, 0, 256, 512, 256);

            var native = brush.Unwrap<D2D.LinearGradientBrush>();

            _renderTarget.FillRectangle(
                new DX.RectangleF(0, 0, RenderContext.Width, RenderContext.Height),
                native);

            _renderTarget.EndDraw();

            brush.Dispose();
        }

        private void Run()
        {
            var wndClass = new WndClass
            {
                lpszClassName = "d2d",
                hInstance = Marshal.GetHINSTANCE(GetType().Module),
                lpfnWndProc = _proc = WndProc
            };

            var cls = RegisterClass(ref wndClass);

            if (cls == 0)
            {
                var code = Marshal.GetLastWin32Error();

                throw new Win32Exception(code);
            }

            _hwnd = CreateWindowEx(
                0, cls, "",
                WindowStyles.Maximize,
                0, 0,
                512,
                512,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                0);

            if (_hwnd == IntPtr.Zero)
            {
                var code = Marshal.GetLastWin32Error();

                throw new Win32Exception(code);
            }

            /*var _d2dFactory = new D2D.Factory(D2D.FactoryType.MultiThreaded);

            var width = (int) (512 * _d2dFactory.DesktopDpi.Width / 96.0);
            var height = (int) (512 * _d2dFactory.DesktopDpi.Height / 96.0);

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
                Hwnd = _hwnd,
                PixelSize = new DX.Size2(width, height),
                PresentOptions = D2D.PresentOptions.None
            };

            _renderTarget = new D2D.WindowRenderTarget(_d2dFactory, rtp, hrtp);

            RenderContext = new Direct2DRenderContext(_renderTarget);*/

            // Process.Start("helper.exe");

            var accent = new AccentPolicy();
            var accentStructSize = Marshal.SizeOf(accent);
            accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(_hwnd, ref data);

            ShowWindow(_hwnd, 3);
            UpdateWindow(_hwnd);

            while (GetMessage(out var msg, IntPtr.Zero, 0, 0))
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }

        public IntPtr WndProc(IntPtr hwnd, WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WindowMessage.Paint:
                    if (_renderTarget == null) goto default;

                    BeginPaint(hwnd, out var paintStruct);

                    Paint();

                    EndPaint(hwnd, ref paintStruct);

                    InvalidateRect(_hwnd, IntPtr.Zero, false);
                    break;
                case WindowMessage.KeyDown:
                case WindowMessage.SysKeyDown:
                case WindowMessage.KeyUp:
                case WindowMessage.SysKeyUp:
                case WindowMessage.Input:
                    return IntPtr.Zero;
                case WindowMessage.Activate:
                    if (wParam == IntPtr.Zero) // WA_INACTIVE
                        SetActiveWindow(_hwnd);

                    break;
                case WindowMessage.Close:
                    // you can't close me bitch
                    return (IntPtr) 1;
                case WindowMessage.Destroy:
                    _evt.Set();
                    return (IntPtr)1;
                default:
                    return DefWindowProc(hwnd, msg, wParam, lParam);
            }

            return IntPtr.Zero;
        }
    }

    
}