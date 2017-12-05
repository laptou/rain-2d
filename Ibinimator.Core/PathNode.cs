using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Ibinimator.Core {
    public struct PathNode
    {
        public Vector2 Position { get; }

        public Vector2? IncomingControl { get; }

        public Vector2? OutgoingControl { get; }

        public int Index { get; }

        public PathFigureEnd? FigureEnd { get; }

        public PathNode(int index, Vector2 position) : this()
        {
            Index = index;
            Position = position;
        }

        public PathNode(int index, Vector2 position, Vector2 incomingControl) : this(index, position)
        {
            IncomingControl = incomingControl;
        }

        public PathNode(int index, Vector2 position, Vector2 incomingControl, Vector2 outgoingControl) :
            this(index, position, incomingControl)
        {
            OutgoingControl = outgoingControl;
        }

        public PathNode(
            int index,
            Vector2 position,
            Vector2? incomingControl,
            Vector2? outgoingControl,
            PathFigureEnd? figureEnd) : this(
            index, position)
        {
            IncomingControl = incomingControl;
            OutgoingControl = outgoingControl;
            FigureEnd = figureEnd;
        }

        public override string ToString()
        {
            var str = "[" + Index + "]:: ";

            if (IncomingControl != null)
                str += "In: " + IncomingControl.Value + " ";

            str += "Position: " + Position + " ";

            if (OutgoingControl != null)
                str += "Out: " + OutgoingControl.Value + " ";

            if (FigureEnd != null)
                str += "End: " + FigureEnd.Value + " ";

            return str.Trim();
        }
    }
}