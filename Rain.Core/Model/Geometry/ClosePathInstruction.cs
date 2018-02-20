using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Geometry
{
    [DebuggerDisplay("Z")]
    public class ClosePathInstruction : PathInstruction
    {
        public ClosePathInstruction(bool open) { Open = open; }

        public bool Open { get; }
    }
}