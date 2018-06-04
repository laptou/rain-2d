using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rain.Core.Model.Imaging
{
    public struct ImageFormat
    {
        public ImageEncoding Encoding { get; set; }

        public Vector2 Dpi { get; set; }
    }
}