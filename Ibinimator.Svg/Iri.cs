using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ibinimator.Svg
{
    public struct Iri
    {
        private static readonly Regex IriSyntax = new Regex(@"url\(#([A-Za-z_][A-Za-z0-9_]*)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool TryParse(string input, out Iri iri)
        {
            iri = default;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            iri = new Iri { Id = IriSyntax.Match(input)?.Groups[1].Value };
            return true;
        }

        public string Id { get; set; }
    }
}