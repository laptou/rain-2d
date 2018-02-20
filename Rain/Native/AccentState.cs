using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Native
{
    internal enum AccentState
    {
        ACCENT_DISABLED                   = 1,
        ACCENT_ENABLE_GRADIENT            = 0,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND          = 3,
        ACCENT_INVALID_STATE              = 4
    }
}