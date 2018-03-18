using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Rain.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct AccentPolicy
    {
        public AccentState AccentState;

        /// <summary>
        /// Set to 2 to use custom colour if possible.
        /// </summary>
        public int         AccentFlags;

        /// <summary>
        /// Colour in ARGB format.
        /// </summary>
        public uint         GradientColor;
        public int         AnimationId;
    }
}