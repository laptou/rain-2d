using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Native
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
        Clipsiblings = 0x04000000,
        Clipchildren = 0x02000000,
        Maximize     = 0x01000000,
        Border       = 0x00800000,
        Dlgframe     = 0x00400000,
        Vscroll      = 0x00200000,
        Hscroll      = 0x00100000,
        Sysmenu      = 0x00080000,
        Thickframe   = 0x00040000,
        Group        = 0x00020000,
        Tabstop      = 0x00010000,

        Minimizebox = 0x00020000,
        Maximizebox = 0x00010000,

        Caption     = Border | Dlgframe,
        Tiled       = Overlapped,
        Iconic      = Minimize,
        Sizebox     = Thickframe,
        Tiledwindow = Overlappedwindow,

        Overlappedwindow = Overlapped | Caption | Sysmenu | Thickframe | Minimizebox | Maximizebox,
        Popupwindow      = Popup | Border | Sysmenu,
        Childwindow      = Child,

        //Extended Window Styles

        ExDlgmodalframe  = 0x00000001,
        ExNoparentnotify = 0x00000004,
        ExTopmost        = 0x00000008,
        ExAcceptfiles    = 0x00000010,
        ExTransparent    = 0x00000020,

        //#if(WINVER >= 0x0400)

        ExMdichild    = 0x00000040,
        ExToolwindow  = 0x00000080,
        ExWindowedge  = 0x00000100,
        ExClientedge  = 0x00000200,
        ExContexthelp = 0x00000400,

        ExRight          = 0x00001000,
        ExLeft           = 0x00000000,
        ExRtlreading     = 0x00002000,
        ExLtrreading     = 0x00000000,
        ExLeftscrollbar  = 0x00004000,
        ExRightscrollbar = 0x00000000,

        ExControlparent = 0x00010000,
        ExStaticedge    = 0x00020000,
        ExAppwindow     = 0x00040000,

        ExOverlappedwindow = ExWindowedge | ExClientedge,
        ExPalettewindow    = ExWindowedge | ExToolwindow | ExTopmost,

        //#endif /* WINVER >= 0x0400 */

        //#if(WIN32WINNT >= 0x0500)

        ExLayered = 0x00080000,

        //#endif /* WIN32WINNT >= 0x0500 */

        //#if(WINVER >= 0x0500)

        ExNoinheritlayout = 0x00100000, // Disable inheritence of mirroring by children
        ExLayoutrtl       = 0x00400000, // Right to left mirroring

        //#endif /* WINVER >= 0x0500 */

        //#if(WIN32WINNT >= 0x0500)

        ExComposited = 0x02000000,
        ExNoactivate = 0x08000000

        //#endif /* WIN32WINNT >= 0x0500 */
    }
}