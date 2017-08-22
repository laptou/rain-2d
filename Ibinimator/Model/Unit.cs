using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Model
{
    public enum Unit
    {
        // angle
        Degrees,
        Radians,

        // length
        Pixels,
        Centimeters,
        Inches,

        // time
        Milliseconds,
        Frames,
        Seconds,
        Minutes,
        Hours
    }

    public enum UnitType
    {
        Angle,
        Length,
        Time
    }

    public static class UnitExtensions
    {
        public static UnitType GetUnitType(this Unit unit)
        {
            switch (unit)
            {
                case Unit.Degrees:
                case Unit.Radians:
                    return UnitType.Angle;
                case Unit.Pixels:
                case Unit.Centimeters:
                case Unit.Inches:
                    return UnitType.Length;
                case Unit.Milliseconds:
                case Unit.Frames:
                case Unit.Seconds:
                case Unit.Minutes:
                case Unit.Hours:
                    return UnitType.Time;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }
        }
    }
}