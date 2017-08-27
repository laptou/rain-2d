using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Model
{
    public enum Unit
    {
        None,

        // angle
        Degrees,
        Radians,

        // length
        Pixels,
        Points,
        Centimeters,
        Inches,
        Millimeters,

        // time
        Milliseconds,
        Frames,
        Seconds,
        Minutes,
        Hours
    }

    public enum UnitType
    {
        None,
        Angle,
        Length,
        Time
    }

    public static class UnitExtensions
    {
        public static Unit GetBaseUnit(this UnitType type)
        {
            switch (type)
            {
                case UnitType.None: return Unit.None;
                case UnitType.Angle: return Unit.Radians;
                case UnitType.Length: return Unit.Pixels;
                case UnitType.Time: return Unit.Seconds;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static UnitType GetUnitType(this Unit unit)
        {
            switch (unit)
            {
                case Unit.None:
                    return UnitType.None;

                case Unit.Degrees:
                case Unit.Radians:
                    return UnitType.Angle;

                case Unit.Pixels:
                case Unit.Points:
                case Unit.Millimeters:
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