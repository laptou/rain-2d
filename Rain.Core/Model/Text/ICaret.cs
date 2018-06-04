using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rain.Core.Model.Text
{
    public interface ICaret : IDisposable
    {
        long BlinkPeriod { get; set; }
        Vector2 Position { get; set; }
        bool Visible { get; set; }
    }
}