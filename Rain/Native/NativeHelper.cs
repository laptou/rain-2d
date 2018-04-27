using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Rain.Native
{
    internal static class NativeHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe IntPtr Blit<T>(this T value, out int size) where T : struct
        {
            size = SizeOf<T>();

            var bytePtr = (byte*) Marshal.AllocHGlobal(size);

            var valueref = __makeref(value);
            var valuePtr = (byte*) *((IntPtr*) &valueref);

            for (var i = 0; i < size; ++i)
                bytePtr[i] = valuePtr[i];

            return (IntPtr) bytePtr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T Blit<T>(this IntPtr ptr) where T : struct
        {
            var size = SizeOf<T>();
            var bytePtr = (byte*) ptr;

            var value = default(T);
            var valueref = __makeref(value);
            var valuePtr = (byte*) *((IntPtr*) &valueref);

            for (var i = 0; i < size; ++i)
                valuePtr[i] = bytePtr[i];

            return value;
        }

        public static void CheckError()
        {
            var error = Marshal.GetLastWin32Error();

            if (error != 0) throw new Win32Exception(error);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void CopyMemory([In] IntPtr dest, [In] IntPtr src, [In] uint length);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe TOut Reinterpret<TIn, TOut>(TIn curValue, int sizeBytes)
            where TIn : struct where TOut : struct
        {
            var result = default(TOut);

            var resultRef = __makeref(result);
            var resultPtr = (byte*) *((IntPtr*) &resultRef);

            var curValueRef = __makeref(curValue);
            var curValuePtr = (byte*) *((IntPtr*) &curValueRef);

            for (var i = 0; i < sizeBytes; ++i)
                resultPtr[i] = curValuePtr[i];

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut Reinterpret<TIn, TOut>(TIn curValue)
            where TIn : struct where TOut : struct
        {
            return Reinterpret<TIn, TOut>(curValue, SizeOf<TIn>());
        }

        public static unsafe int SizeOf<T>() where T : struct
        {
            var type = typeof(T);

            var typeCode = Type.GetTypeCode(type);

            switch (typeCode)
            {
                case TypeCode.Boolean:

                    return sizeof(bool);
                case TypeCode.Char:

                    return sizeof(char);
                case TypeCode.SByte:

                    return sizeof(sbyte);
                case TypeCode.Byte:

                    return sizeof(byte);
                case TypeCode.Int16:

                    return sizeof(short);
                case TypeCode.UInt16:

                    return sizeof(ushort);
                case TypeCode.Int32:

                    return sizeof(int);
                case TypeCode.UInt32:

                    return sizeof(uint);
                case TypeCode.Int64:

                    return sizeof(long);
                case TypeCode.UInt64:

                    return sizeof(ulong);
                case TypeCode.Single:

                    return sizeof(float);
                case TypeCode.Double:

                    return sizeof(double);
                case TypeCode.Decimal:

                    return sizeof(decimal);
                case TypeCode.DateTime:

                    return sizeof(DateTime);
                default:
                    var tArray = new T[2];
                    var tArrayPinned = GCHandle.Alloc(tArray, GCHandleType.Pinned);

                    try
                    {
                        var tRef0 = __makeref(tArray[0]);
                        var tRef1 = __makeref(tArray[1]);
                        var ptrToT0 = *((IntPtr*) &tRef0);
                        var ptrToT1 = *((IntPtr*) &tRef1);

                        return (int) ((byte*) ptrToT1 - (byte*) ptrToT0);
                    }
                    finally
                    {
                        tArrayPinned.Free();
                    }
            }
        }

        public static IntPtr ToPtr<T>(this T valueType) where T : struct
        {
            return valueType.ToPtr(out _);
        }

        public static IntPtr ToPtr(this ValueType valueType) { return valueType.ToPtr(out _); }

        public static IntPtr ToPtr<T>(this T valueType, out int size) where T : struct
        {
            size = SizeOf<T>();
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(valueType, ptr, false);

            return ptr;
        }

        public static IntPtr ToPtr(this ValueType valueType, out int size)
        {
            size = Marshal.SizeOf(valueType);
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(valueType, ptr, false);

            return ptr;
        }

        public static SmartPtr ToSmartPtr<T>(this T valueType) where T : struct
        {
            return SmartPtr.Alloc(valueType);
        }

        public static SmartPtr ToSmartPtr(this ValueType valueType)
        {
            return SmartPtr.Alloc(valueType);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int WaitForSingleObjectEx(
            [In] IntPtr hHandle, [In] uint dwMilliseconds, [In] bool bAlertable);
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