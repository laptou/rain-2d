using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Utility;

namespace Ibinimator.Renderer.Model
{
    public interface IContainerLayer : ILayer
    {
        ObservableList<Layer> SubLayers { get; }

        event EventHandler<Layer> LayerAdded;
        event EventHandler<Layer> LayerRemoved;

        void Add(Layer child, int index = -1);
        IEnumerable<Layer> Flatten();
        void Remove(Layer child);
    }
}