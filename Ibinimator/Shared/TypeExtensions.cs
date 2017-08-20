using System;

namespace Ibinimator.Shared
{
    public static class TypeExtensions
    {
        public static bool IsElementary(this Type type)
        {
            return type == typeof(float) ||
                   type == typeof(bool) ||
                   type == typeof(byte) ||
                   type == typeof(char) ||
                   type == typeof(decimal) ||
                   type == typeof(double) ||
                   type == typeof(int) ||
                   type == typeof(long) ||
                   type == typeof(short) ||
                   type == typeof(string) ||
                   type == typeof(ushort) ||
                   type == typeof(ulong) ||
                   type == typeof(uint) ||
                   type == typeof(sbyte);
        }
    }
}
