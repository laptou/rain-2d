﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpDX.DirectWrite;

namespace Ibinimator.Utility
{
    public static class DirectWriteExtensions
    {
        public static string ToCurrentCulture(this LocalizedStrings ls)
        {
            ls.FindLocaleName(Thread.CurrentThread.CurrentUICulture.Name, out var i);

            return ls.GetString(i);
        }
    }
}