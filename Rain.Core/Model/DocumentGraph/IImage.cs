using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.DocumentGraph
{
    public interface IImageLayer : ILayer
    {
        event EventHandler ImageChanged;
        Imaging.IRenderImage GetImage(IArtContext ctx);
    }
}