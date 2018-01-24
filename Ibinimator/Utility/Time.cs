﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Ibinimator.Service
{
    public class Time
    {
        public static long DoubleClick => GetDoubleClickTime();
        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        [DllImport("user32.dll")]
        private static extern uint GetDoubleClickTime();
    }
}