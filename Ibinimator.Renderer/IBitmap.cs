using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    public interface IBitmap : IResource
    {
        float Width { get; }
        float Height { get; }

        int PixelWidth { get; }
        int PixelHeight { get; }

        float Dpi { get; }
    }
}
