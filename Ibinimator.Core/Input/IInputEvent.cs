using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core.Input
{
    public interface IInputEvent
    {
        long Timestamp { get; }
    }
}