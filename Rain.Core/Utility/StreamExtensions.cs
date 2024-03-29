﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rain.Core.Utility
{
    public static class StreamExtensions
    {
        public static string ReadAsString(this Stream stream)
        {
            var sr = new StreamReader(stream);

            return sr.ReadToEnd();
        }

        public static async Task<string> ReadAsStringAsync(this Stream stream)
        {
            var sr = new StreamReader(stream);

            return await sr.ReadToEndAsync();
        }

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