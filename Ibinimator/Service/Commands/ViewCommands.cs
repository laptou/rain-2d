using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.View;
using Ibinimator.ViewModel;

namespace Ibinimator.Service.Commands
{
    public static class ViewCommands
    {
        public static DelegateCommand<object> LicenseCommand = new DelegateCommand<object>(License, null);

        private static void License(object obj)
        {
            App.Dispatcher.Invoke(() =>
            {
                new LicenseView().ShowDialog();
            });
        }
    }
}