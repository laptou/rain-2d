﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Formatter.Svg.Enums
{
    public struct Cursor
    {
        #region Value enum

        public enum Value
        {
            Auto = 0,
            Crosshair,
            Default = 0,
            Pointer,
            Move,
            EResize,
            NEResize,
            NWResize,
            NResize,
            SEResize = NWResize,
            SWREsize = NEResize,
            SResize  = NResize,
            WResize  = EResize,
            Text,
            Wait,
            Help
        }

        #endregion

        public Value EnumValue { get; set; }

        public Uri UriValue { get; set; }
    }
}