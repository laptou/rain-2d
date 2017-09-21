using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Svg.Reader
{
    public struct Cursor
    {
        public enum Value
        {
            Auto = 0,
            Crosshair,
            Default = 0,
            Pointer,
            Move,
            EResize,
            NEResize,
            NWResize,
            NResize,
            SEResize = NWResize,
            SWREsize = NEResize,
            SResize = NResize,
            WResize = EResize,
            Text,
            Wait,
            Help
        }

        public Value EnumValue { get; set; }

        public Iri IriValue { get; set; }
    }
}