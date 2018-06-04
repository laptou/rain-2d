using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Imaging
{
    public interface IImageLock : IDisposable
    {
        IImageFrame ImageFrame { get; }

        T[] GetPixels<T>(int count);
        void SetPixels<T>(T[] data);
    }
}