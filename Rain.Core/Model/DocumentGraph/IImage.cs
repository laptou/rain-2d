using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core.Model.Imaging;

namespace Rain.Core.Model.DocumentGraph
{
    public interface IImageLayer : ILayer
    {
        event EventHandler ImageChanged;
        IRenderImage GetImage(IArtContext ctx);
    }
}