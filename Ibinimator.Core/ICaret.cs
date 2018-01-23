using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public interface ICaret : IDisposable
    {
        Vector2 Position { get; set; }
        bool Visible { get; set; }
    }
}