using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Utility;

namespace Rain.Core.Model.DocumentGraph
{
    public interface IContainerLayer : ILayer
    {
        ILayer this[int index] { get; }
        ILayer this[Guid id] { get; }
        ObservableList<ILayer> SubLayers { get; }

        event EventHandler<ILayer> LayerAdded;
        event EventHandler<ILayer> LayerRemoved;

        void Add(ILayer child, int index = -1);
        void Remove(ILayer child);
    }
}