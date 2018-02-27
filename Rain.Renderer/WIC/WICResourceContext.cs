using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rain.Core;
using Rain.Core.Model.Imaging;
using Rain.Renderer.Direct2D;

namespace Rain.Renderer.WIC
{
    public class WICResourceContext : ResourceContext
    {
        /// <inheritdoc />
        public override IImage LoadImageFromFilename(string filename)
        {
            var img = new Image();
            img.Load(filename);

            return img;
        }

        /// <inheritdoc />
        public override IImage LoadImageFromStream(Stream stream)
        {
            var img = new Image();
            img.Load(stream);

            return img;
        }
    }
}
