using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Ibinimator.Core.Model;

namespace Ibinimator.Model {}

namespace Ibinimator.Native
{
    internal static class NativeHelper
    {
        public static void CheckError()
        {
            var error = Marshal.GetLastWin32Error();

            if (error != 0) throw new Win32Exception(error);
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
    }
}