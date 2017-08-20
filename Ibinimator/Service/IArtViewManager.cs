using System.ComponentModel;
using Ibinimator.View.Control;

namespace Ibinimator.Service
{
    public interface IArtViewManager : INotifyPropertyChanged
    {
        ArtView ArtView { get; }
    }
}