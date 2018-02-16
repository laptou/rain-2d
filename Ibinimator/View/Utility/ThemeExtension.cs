using System;
using System.Collections.Generic;

using Ibinimator.Renderer.WPF;

using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media;

using Ibinimator.Service;

using Color = Ibinimator.Core.Model.Color;

namespace Ibinimator.View.Utility
{
    public class ThemeExtension : MarkupExtension
    {
        private static readonly Regex Func =
            new Regex(@"^([A-Za-z0-9\-_]+)\(([A-Za-z0-9_\-\.]+)\)$",
                      RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public ThemeExtension(string path) { Path = path; }

        public string Path { get; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            string path, modifier = null;

            if (Func.IsMatch(Path))
            {
                var match = Func.Match(Path);
                modifier = match.Groups[1].Value;
                path = match.Groups[2].Value;
            }
            else
            {
                path = Path;
            }

            var val = AppSettings.Current.Theme[path];

            if (val is Color color)
                switch (modifier)
                {
                    case "color":

                        return color.Convert();
                    default:

                        return new SolidColorBrush(color.Convert());
                }

            return val;
        }
    }
}