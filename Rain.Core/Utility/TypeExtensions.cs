using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Utility
{
    public static class TypeExtensions
    {
        public static bool IsElementary(this Type type)
        {
            return Type.GetTypeCode(type) != TypeCode.Object;
        }

        public static bool IsNumeric(this Type type)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:

                    return true;
                default:

                    return false;
            }
        }
    }
}