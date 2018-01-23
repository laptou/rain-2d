using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Ibinimator.Core;

using static Ibinimator.Native.NativeHelper;

namespace Ibinimator.Native
{
    public class Caret : ICaret
    {
        private readonly IntPtr _hWnd;
        private          bool   _visible;
        private          bool   _disposed;
        private readonly int    _width;
        private readonly int    _height;

        public Caret(IntPtr hWnd, int width, int height)
        {
            _hWnd = hWnd;
            _width = width;
            _height = height;


            if (!CaretHelper.CreateCaret(_hWnd, IntPtr.Zero, width, height))
                CheckError();

            _disposed = false;
        }

        public Vector2 GetPosition()
        {
            if (_disposed) throw new NullReferenceException();

            var scale = WindowHelper.GetDpiForWindow(_hWnd) / 96f;
            CaretHelper.GetCaretPos(out var pt);

            return new Vector2(pt.x / scale, pt.y / scale);
        }

        public void SetPosition(float x, float y)
        {
            if (_disposed) throw new NullReferenceException();

            var scale = WindowHelper.GetDpiForWindow(_hWnd) / 96f;

            if(!CaretHelper.SetCaretPos((int) (x * scale),
                                    (int) (y * scale)))
                CheckError();
        }

        ~Caret() { ReleaseUnmanagedResources(); }

        public void Hide()
        {
            if (!_visible) return;

            CaretHelper.HideCaret(_hWnd);
            _visible = false;
        }

        public void Show()
        {
            if (_visible) return;

            CaretHelper.ShowCaret(_hWnd);
            _visible = true;
        }

        private void ReleaseUnmanagedResources() { CaretHelper.DestroyCaret(); }

        #region IDisposable Members

        public void Dispose()
        {
            _disposed = true;
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        #endregion

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
    }
}