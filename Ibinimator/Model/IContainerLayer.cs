using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Shared;

namespace Ibinimator.Model
{
    public interface IContainerLayer : ILayer
    {
        ObservableList<Layer> SubLayers { get; }

        event EventHandler<Layer> LayerAdded;
        event EventHandler<Layer> LayerRemoved;

        void Add(Layer child, int index = 0);
        IEnumerable<Layer> Flatten();
        void Remove(Layer child);
    }
}