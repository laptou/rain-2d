using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Input
{
    public abstract class InputEventBase : IInputEvent
    {
        #region IInputEvent Members

        public long Timestamp { get; } = Stopwatch.GetTimestamp();

        #endregion
    }
}