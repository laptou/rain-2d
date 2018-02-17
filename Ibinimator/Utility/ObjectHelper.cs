﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Core.Model.DocumentGraph;

namespace Ibinimator.Utility
{
    public class ObjectHelper
    {
        public T CreateObject<T>(IArtContext context, IContainerLayer parent = null)
            where T : ILayer, new()
        {
            var layer = new T();

            if (layer is IFilledLayer filled)
                filled.Fill = context.BrushManager.BrushHistory.FirstOrDefault();

            parent?.Add(layer);

            return layer;
        }
    }
}