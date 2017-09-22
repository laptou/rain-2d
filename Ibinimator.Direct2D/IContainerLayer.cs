using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Shared;

namespace Ibinimator.Direct2D
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