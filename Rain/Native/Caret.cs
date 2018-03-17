using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Core.Model.Text;

namespace Rain.Native
{
    public class Caret : ICaret
    {
        private readonly int    _height;
        private readonly IntPtr _hWnd;
        private readonly int    _width;
        private          bool   _disposed;
        private          bool   _visible;
        private          float  _x;
        private          float  _y;

        public Caret(IntPtr hWnd, int width, int height)
        {
            _hWnd = hWnd;
            _width = width;
            _height = height;


            if (!CaretHelper.CreateCaret(_hWnd, IntPtr.Zero, width, height))
                NativeHelper.CheckError();

            _disposed = false;
        }

        ~Caret() { ReleaseUnmanagedResources(); }

        public Vector2 GetPosition()
        {
            if (_disposed) throw new NullReferenceException();

            if (!_visible) return new Vector2(_x, _y);

            return App.Dispatcher.Invoke(() =>
                                         {
                                             var scale = WindowHelper.GetDpiForWindow(_hWnd) / 96f;
                                             if (!CaretHelper.GetCaretPos(out var pt))
                                                 NativeHelper.CheckError();

                                             return new Vector2(pt.x / scale, pt.y / scale);
                                         });
        }

        public bool Hide()
        {
            if (!_visible) return true;

            return App.Dispatcher.Invoke(() =>
                                         {
                                             if (!CaretHelper.HideCaret(_hWnd))
                                             {
                                                 NativeHelper.CheckError();

                                                 return false;
                                             }

                                             _visible = false;

                                             return true;
                                         });
        }

        public void SetPosition(float x, float y)
        {
            if (_disposed) throw new NullReferenceException();

            (_x, _y) = (x, y);

            App.Dispatcher.Invoke(() =>
                                  {
                                      var scale = WindowHelper.GetDpiForWindow(_hWnd) / 96f;

                                      if (!CaretHelper.SetCaretPos(
                                              (int) (x * scale),
                                              (int) (y * scale)))
                                          NativeHelper.CheckError();
                                  });
        }

        public bool Show()
        {
            if (_visible) return true;

            return App.Dispatcher.Invoke(() =>
                                         {
                                             if (!CaretHelper.ShowCaret(_hWnd))
                                             {
                                                 NativeHelper.CheckError();

                                                 return false;
                                             }

                                             if (GetPosition() != new Vector2(_x, _y))
                                                 SetPosition(_x, _y);

                                             _visible = true;

                                             return true;
                                         });
        }

        private void ReleaseUnmanagedResources()
        {
            App.Dispatcher.Invoke(() =>
                                  {
                                      if (!CaretHelper.DestroyCaret())
                                          NativeHelper.CheckError();
                                  });
        }

        #region ICaret Members

        public void Dispose()
        {
            _disposed = true;
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public Vector2 Position
        {
            get => GetPosition();
            set => SetPosition(value.X, value.Y);
        }


        /// <inheritdoc />
        public bool Visible
        {
            get => _visible;
            set
            {
                if (value) Show();
                else Hide();
            }
        }

        /// <inheritdoc />
        public long BlinkPeriod
        {
            get => CaretHelper.GetCaretBlinkTime();
            set => CaretHelper.SetCaretBlinkTime((uint)value);
        }

        #endregion
    }
}