using System;
using System.Collections.Generic;

namespace Rain.Core.Model.Imaging
{
    public interface IImage : IDisposable
    {
        bool Alpha { get; }

        ColorFormat ColorFormat { get; }

        IReadOnlyList<IImageFrame> Frames { get; }

        ImageFormat ImageFormat { get; }
    }
}