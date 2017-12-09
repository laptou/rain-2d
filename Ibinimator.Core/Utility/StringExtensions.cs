using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ibinimator.Core.Utility
{
    public static class StringExtensions
    {
        public static string ToTitle(this string s)
        {
            return Thread.CurrentThread.CurrentUICulture.TextInfo.ToTitleCase(s);
        }

        public static string Dedash(this string s) { return s.ToTitle().Replace("-", ""); }

        public static string DePascalize(this Enum e)
        {
            var s = e.ToString();
            var sb = new StringBuilder();

            sb.Append(s[0]);

            var acronym = true;
            foreach (var c in s.Skip(1))
            {
                if (char.IsUpper(c))
                {
                    if (!acronym)
                        sb.Append(' ');

                    sb.Append(c);
                    acronym = true;
                }
                else
                {
                    sb.Append(c);
                    acronym = false;
                }
            }

            return sb.ToString();
        }

        public static string Truncate(this string s, int length, string terminator = "...")
        {
            if (s.Length < length)
                return s;

            return s.Substring(0, length) + terminator;
        }
    }
}