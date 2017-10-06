using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Renderer
{
    public class ClosePathInstruction : PathInstruction
    {
        public ClosePathInstruction(bool open)
        {
            Open = open;
        }

        public bool Open { get; }
    }
}