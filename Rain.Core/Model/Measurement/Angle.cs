﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Model.Measurement
{
    public struct Angle
    {
        public float Magnitude { get; set; }

        public AngleUnit Unit { get; set; }

        public static Angle Convert(Angle length, AngleUnit target) { throw new NotImplementedException(); }
    }
}