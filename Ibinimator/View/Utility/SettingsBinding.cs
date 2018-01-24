using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Markup;

using Ibinimator.Service;

namespace Ibinimator.View.Utility
{
    public class SettingsBinding : MarkupExtension
    {
        public SettingsBinding(string path) { Path = path; }

        public string Path { get; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Settings.GetObject(Path);
        }
    }
}