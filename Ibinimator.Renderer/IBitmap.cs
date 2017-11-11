using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    public interface IBitmap : IResource
    {
        float Dpi { get; }
        float Height { get; }
        int PixelHeight { get; }

        int PixelWidth { get; }
        float Width { get; }
    }
}