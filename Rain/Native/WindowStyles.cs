using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Native
{
    [Flags]
    public enum WindowStyles : uint
    {
        Overlapped   = 0x00000000,
        Popup        = 0x80000000,
        Child        = 0x40000000,
        Minimize     = 0x20000000,
        Visible      = 0x10000000,
        Disabled     = 0x08000000,
        ClipSiblings = 0x04000000,
        ClipChildren = 0x02000000,
        Maximize     = 0x01000000,
        Border       = 0x00800000,
        DlgFrame     = 0x00400000,
        VScroll      = 0x00200000,
        HScroll      = 0x00100000,
        SysMenu      = 0x00080000,
        ThickFrame   = 0x00040000,
        Group        = 0x00020000,
        Tabstop      = 0x00010000,

        MinimizeBox = 0x00020000,
        MaximizeBox = 0x00010000,

        Caption     = Border | DlgFrame,
        Tiled       = Overlapped,
        Iconic      = Minimize,
        Sizebox     = ThickFrame,
        TiledWindow = OverlappedWindow,

        OverlappedWindow = Overlapped | Caption | SysMenu | ThickFrame | MinimizeBox | MaximizeBox,
        PopupWindow      = Popup | Border | SysMenu,
        ChildWindow      = Child
    }
}