using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core.Model;

namespace Ibinimator.Core
{
    public interface IBitmap : IResource
    {
        float Dpi { get; }
        float Height { get; }
        int PixelHeight { get; }

        int PixelWidth { get; }
        float Width { get; }

        /// <summary>
        ///     Gets the native object representing the brush.
        /// </summary>
        /// <typeparam name="T">The type of object that is expected.</typeparam>
        /// <returns>The native object representing the brush.</returns>
        T Unwrap<T>() where T : class;
    }
}