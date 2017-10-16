using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Ibinimator.Core.Utility
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