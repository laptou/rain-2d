using Ibinimator.View;
using Ibinimator.ViewModel;

namespace Ibinimator
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