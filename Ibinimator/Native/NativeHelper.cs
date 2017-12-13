using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ibinimator.Native
{
    public static class NativeHelper
    {
        /// <summary>
        ///     Gets high bits values of the pointer.
        /// </summary>
        public static int HighWord(IntPtr ptr)
        {
            var val32 = (int)ptr;
            return (val32 >> 16) & 0xFFFF;
        }

        /// <summary>
        ///     Gets low bits values of the pointer.
        /// </summary>
        public static int LowWord(IntPtr ptr)
        {
            var val32 = (int)ptr;
            return val32 & 0xFFFF;
        }
    }
}
