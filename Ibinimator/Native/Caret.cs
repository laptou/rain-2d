using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using static Ibinimator.Native.NativeHelper;

namespace Ibinimator.Native
{
    public class Caret : IDisposable
    {
        private readonly IntPtr _hWnd;
        private          bool   _visible;

        public Caret(int width, int height)
        {
            _hWnd = WindowHelper.GetActiveWindow();

            if (!CaretHelper.CreateCaret(_hWnd, IntPtr.Zero, width, height))
                CheckError();
        }

        public (int X, int Y) GetPosition()
        {
            CaretHelper.GetCaretPos(out var pt);

            return (pt.x, pt.y);
        }

        public void SetPosition(int x, int y) { CaretHelper.SetCaretPos(x, y); }

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
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}