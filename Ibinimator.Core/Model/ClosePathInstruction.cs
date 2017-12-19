using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core.Model
{
    [DebuggerDisplay("Z")]
    public class ClosePathInstruction : PathInstruction
    {
        public ClosePathInstruction(bool open) { Open = open; }

        public bool Open { get; }
    }
}