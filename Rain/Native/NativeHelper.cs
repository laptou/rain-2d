using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Rain.Native
{
    internal static class NativeHelper
    {
        public static void CheckError()
        {
            var error = Marshal.GetLastWin32Error();

            if (error != 0) throw new Win32Exception(error);
        }

        public static Vector2 GetCoordinates(IntPtr lParam, float dpi)
        {
            var x = LowWord(lParam) / dpi * 96f;
            var y = HighWord(lParam) / dpi * 96f;

            return new Vector2(x, y);
        }

        public static Vector2 GetCoordinates(IntPtr lParam, float dpi, IntPtr hWnd)
        {
            var pt = new NativePoint {x = LowWord(lParam), y = HighWord(lParam)};
            WindowHelper.ScreenToClient(hWnd, ref pt);
            var x = pt.x / dpi * 96f;
            var y = pt.y / dpi * 96f;

            return new Vector2(x, y);
        }

        /// <summary>
        ///     Gets high bits values of the pointer.
        /// </summary>
        public static short HighWord(IntPtr ptr)
        {
            unchecked
            {
                var val32 = (ulong) ptr;

                return (short) ((val32 & 0xFFFF0000) >> 16);
            }
        }

        /// <summary>
        ///     Gets low bits values of the pointer.
        /// </summary>
        public static short LowWord(IntPtr ptr)
        {
            var val32 = (ulong) ptr;

            return (short) (val32 & 0x0000FFFF);
        }

        public static IntPtr ToPtr(this ValueType valueType) { return valueType.ToPtr(out var _); }

        public static IntPtr ToPtr(this ValueType valueType, out int size)
        {
            size = Marshal.SizeOf(valueType);
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(valueType, ptr, false);

            return ptr;
        }

        public static SmartPtr ToSmartPtr(this ValueType valueType)
        {
            return SmartPtr.Alloc(valueType);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int WaitForSingleObjectEx(
            [In] IntPtr hHandle, [In] uint dwMilliseconds, [In] bool bAlertable);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void CopyMemory(
            [In] IntPtr dest, [In] IntPtr src, [In] uint length);
    }

    public class SmartPtr : IDisposable
    {
        public SmartPtr(IntPtr intPtr, int size)
        {
            Pointer = intPtr;
            Size = size;
        }

        public IntPtr Pointer { get; set; }

        public int Size { get; set; }

        ~SmartPtr() { Dispose(); }

        public static SmartPtr Alloc<T>(T value) where T : struct
        {
            return new SmartPtr(value.ToPtr(out var size), size);
        }

        public static SmartPtr Alloc(ValueType value)
        {
            return new SmartPtr(value.ToPtr(out var size), size);
        }

        public static SmartPtr Alloc(int size)
        {
            return new SmartPtr(Marshal.AllocHGlobal(size), size);
        }

        public static implicit operator IntPtr(SmartPtr ptr) { return ptr.Pointer; }

        #region IDisposable Members

        /// <inheritdoc />
        public void Dispose()
        {
            if (Pointer == IntPtr.Zero) return;

            Marshal.FreeHGlobal(Pointer);
            Pointer = IntPtr.Zero;
        }

        #endregion
    }
}