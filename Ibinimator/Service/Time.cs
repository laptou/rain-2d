using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ibinimator.Service
{
    public class Time
    {
        public static long Now => DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }
}
