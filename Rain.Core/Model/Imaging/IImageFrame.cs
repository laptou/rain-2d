using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Imaging
{
    public interface IImageFrame : IDisposable
    {
        int Height { get; }
        IImage Image { get; }
        int Width { get; }

        IImageLock GetReadLock();
        IImageLock GetWriteLock();
    }
}