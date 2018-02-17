using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Utility;

namespace Ibinimator.Core.Model.DocumentGraph
{
    public interface IContainerLayer : ILayer
    {
        ObservableList<ILayer> SubLayers { get; }

        event EventHandler<ILayer> LayerAdded;
        event EventHandler<ILayer> LayerRemoved;

        void Add(ILayer child, int index = -1);
        void Remove(ILayer child);
    }
}