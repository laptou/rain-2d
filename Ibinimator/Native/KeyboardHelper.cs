using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Ibinimator.Core.Input;

namespace Ibinimator.Native
{
    internal static class KeyboardHelper
    {
        [DllImport("user32.dll")]
        public static extern ushort GetAsyncKeyState([In] int vKey);

        public static ModifierState GetModifierState(IntPtr wParam)
        {
            var mod = NativeHelper.LowWord(wParam);
            var alt = GetAsyncKeyState(0x12); // VK_MENU (Alt)

            return new ModifierState((mod & 0x0008) != 0, // MK_CONTROL
                                     (mod & 0x0004) != 0, // MK_SHIFT
                                     (alt & 0x8000) != 0,
                                     (mod & 0x0001) != 0, // MK_LBUTTON
                                     (mod & 0x0010) != 0, // MK_MBUTTON
                                     (mod & 0x0002) != 0, // MK_RBUTTON
                                     (mod & 0x0020) != 0, // MK_XBUTTON1
                                     (mod & 0x0040) != 0 // MK_XBUTTON2
                );
        }

        public static ModifierState GetModifierState()
        {
            var shift = GetAsyncKeyState(0x10); // VK_SHIFT
            var ctrl = GetAsyncKeyState(0x11); // VK_CONTROL
            var alt = GetAsyncKeyState(0x12); // VK_MENU (Alt)

            var lmb = GetAsyncKeyState(0x01); // VK_LBUTTON
            var rmb = GetAsyncKeyState(0x02); // VK_RBUTTON
            var mmb = GetAsyncKeyState(0x04); // VK_MBUTTON
            var xmb = GetAsyncKeyState(0x05); // VK_XBUTTON1
            var ymb = GetAsyncKeyState(0x06); // VK_XBUTTON2

            return new ModifierState((ctrl & 0x8000) != 0,
                                     (shift & 0x8000) != 0,
                                     (alt & 0x8000) != 0,
                                     (lmb & 0x8000) != 0,
                                     (mmb & 0x8000) != 0,
                                     (rmb & 0x8000) != 0,
                                     (xmb & 0x8000) != 0,
                                     (ymb & 0x8000) != 0);
        }
    }
}