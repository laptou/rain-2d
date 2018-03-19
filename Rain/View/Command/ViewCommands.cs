using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.ViewModel;

namespace Rain.View.Command
{
    public static class ViewCommands
    {
        public static DelegateCommand<object> LicenseCommand =
            new DelegateCommand<object>(License, null);

        private static void License(object obj)
        {
            App.CurrentDispatcher.Invoke(() => { new LicenseView().ShowDialog(); });
        }
    }
}