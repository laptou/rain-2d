﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public interface IEffect
    {
        void SetInput(int index, IBitmap bitmap);
        void SetInput(int index, IEffect effect);

        /// <summary>
        /// Gets the native object representing the effect.
        /// </summary>
        /// <typeparam name="T">The type of object that is expected.</typeparam>
        /// <returns>The native object representing the effect.</returns>
        T Unwrap<T>() where T : class;
    }
}