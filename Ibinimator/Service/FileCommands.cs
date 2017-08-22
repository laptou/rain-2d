using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ibinimator.Model;
using Ibinimator.ViewModel;

namespace Ibinimator.Service
{
    public static class FileCommands
    {
        public static readonly AsyncDelegateCommand<Document> SerializeCommand =
            new AsyncDelegateCommand<Document>(SaveAsync);

        private static async Task SaveAsync(Document doc)
        {
            if (doc.Path == null)
            {
                var sfd = new SaveFileDialog
                {
                    DefaultExt = ".iba",
                    Filter = "Ibinimation Project|.iba"
                };

                await App.Dispatcher.InvokeAsync(() => sfd.ShowDialog());

                doc.Path = sfd.FileName;
            }

            await FileService.SaveAsync(doc);
        }
    }
}