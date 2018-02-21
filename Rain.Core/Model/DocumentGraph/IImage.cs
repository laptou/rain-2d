using System;

namespace Rain.Core.Model.DocumentGraph
{
    public interface IImage : IDisposable
    {
        byte[] Data { get; set; }
    }

    public enum ImageFormat
    {
        RGBA
    }
}