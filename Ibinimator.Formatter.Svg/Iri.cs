using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ibinimator.Formatter.Svg
{
    public struct Iri
    {
        private static readonly Regex IriSyntax =
            new Regex(@"url\(#([A-Za-z_][A-Za-z0-9_]*)\)|#([A-Za-z_][A-Za-z0-9_]*)",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Id { get; set; }

        public static bool TryParse(string input, out Iri iri)
        {
            iri = default;

            if (string.IsNullOrWhiteSpace(input))
                return false;

            var match = IriSyntax.Match(input);

            if (match != null)
            {
                var id = match.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(id))
                    id = match.Groups[2].Value;

                if (string.IsNullOrWhiteSpace(id))
                    return false;

                iri = new Iri {Id = id};

                return true;
            }

            return false;
        }

        public static Iri FromId(string id) { return new Iri {Id = id}; }

        /// <inheritdoc />
        public override string ToString() { return $"{'#' + Id}"; }
    }
}