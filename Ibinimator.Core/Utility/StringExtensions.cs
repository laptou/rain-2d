using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ibinimator.Core.Utility {
    public static class StringExtensions
    {
        public static string ToTitle(this string s)
        {
            return Thread.CurrentThread.CurrentUICulture.TextInfo.ToTitleCase(s);
        }

        public static string Pascalize(this string s)
        {
            return s.ToTitle().Replace("-", "");
        }

        public static string Truncate(this string s, int length, string terminator = "...")
        {
            if (s.Length < length)
                return s;

            return s.Substring(0, length) + terminator;
        }
    }
}