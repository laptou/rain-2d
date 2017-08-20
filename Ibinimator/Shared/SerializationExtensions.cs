using System;
using System.Runtime.Serialization;

namespace Ibinimator.Shared
{
    public static class SerializationExtensions
    {
        public static Guid GetGuid(this SerializationInfo si, string name)
        {
            return (Guid) si.GetValue(name, typeof(Guid));
        }

        public static T GetValue<T>(this SerializationInfo si, string name)
        {
            return (T) si.GetValue(name, typeof(T));
        }
    }
}
