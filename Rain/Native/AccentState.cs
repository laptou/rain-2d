using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Native
{
    internal enum AccentState
    {
        /// <summary>
        /// Solid black background.
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// Solid coloured background.
        /// </summary>
        EnableGradient = 1,

        /// <summary>
        /// Translucent coloured background.
        /// </summary>
        EnableTransparentGradient = 2,

        /// <summary>
        /// Translucent background that blurs what is behind it.
        /// </summary>
        EnableBlurBehind = 3,

        /// <summary>
        /// Fluent coloured background. Warning: This is ACCENT_INVALID_STATE 
        /// on versions of Windows earlier than build 17063.
        /// </summary>
        EnableFluent = 4,

        /// <summary>
        /// Completely transparent background.
        /// </summary>
        InvalidState = 5
    }
}