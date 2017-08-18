using Ibinimator.Model;
using Ibinimator.ViewModel;

namespace Ibinimator.Service
{
    public static class FileCommands
    {
        public static readonly AsyncDelegateCommand<Layer> SerializeCommand = 
            new AsyncDelegateCommand<Layer>(async layer => await FileService.Serialize(layer));
    }
}