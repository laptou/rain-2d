using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Rain.Core.Model.Paint
{
    public interface IBrush : IResource, INotifyPropertyChanged
    {
        /// <summary>
        ///     The opacity of the brush.
        /// </summary>
        float Opacity { get; set; }

        /// <summary>
        ///     The transformation of the brush.
        /// </summary>
        Matrix3x2 Transform { get; set; }

        /// <summary>
        ///     Gets the native object representing the brush.
        /// </summary>
        /// <typeparam name="T">The type of object that is expected.</typeparam>
        /// <returns>The native object representing the brush.</returns>
        T Unwrap<T>() where T : class;
    }
}