using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Markup;

using Rain.Service;

namespace Rain.View.Utility
{
    public class SettingExtension : MarkupExtension
    {
        public SettingExtension(string path) { Path = path; }

        public string Path { get; }

        public override object ProvideValue(IServiceProvider serviceProvider) { return AppSettings.Current[Path]; }
    }
}