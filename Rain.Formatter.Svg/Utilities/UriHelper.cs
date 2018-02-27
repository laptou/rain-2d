using System;
using System.Collections.Generic;
using System.Text;

namespace Rain.Formatter.Svg.Utilities
{
    public static class UriHelper
    {
        public static bool TryParse(string str, out Uri uri)
        {
            return Uri.TryCreate(str, UriKind.Relative, out uri) ||
                   Uri.TryCreate(str, UriKind.Absolute, out uri);
        }

        public static Uri FromId(string id)
        {
            return new Uri("#" + id, UriKind.Relative);
        }

        public static string GetFragment(this Uri uri)
        {
            if (uri.IsAbsoluteUri) return uri.Fragment.Substring(1);

            var temp = new Uri(new Uri("http://example.com/"), uri);

            return temp.Fragment.Substring(1);
        }
    }
}