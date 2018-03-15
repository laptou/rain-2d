using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rain.Core.Input
{
    public class DropEvent : InputEventBase
    {
        public DropEvent(IReadOnlyCollection<string> fileNames, Vector2 position)
        {
            FileNames = fileNames;
            Position = position;
        }

        public IReadOnlyCollection<string> FileNames { get; }

        public Vector2 Position { get; }
    }
}