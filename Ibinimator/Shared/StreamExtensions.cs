
using System;
using System.IO;

namespace Ibinimator.Shared
{
    public static class StreamExtensions
    {
        public static int ReadInt(this Stream stream)
        {
            var buf = new byte[4];
            stream.Read(buf, 0, 4);
            return BitConverter.ToInt32(buf, 0);
        }

        public static void WriteInt(this Stream stream, int i)
        {
            var buf = new byte[4];
            BitConverter.GetBytes(i);
            stream.Write(buf, 0, 4);
        }
    }
}
