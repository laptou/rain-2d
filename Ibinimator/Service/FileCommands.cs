using Ibinimator.Model;
using Ibinimator.ViewModel;

namespace Ibinimator.Service
{
    public static class FileCommands
    {
        public static readonly DelegateCommand<Layer> SerializeCommand = new DelegateCommand<Layer>(FileService.Serialize, l => true);
    }
}