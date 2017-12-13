﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Ibinimator.Core.Model;

namespace Ibinimator.Core
{
    public interface IViewManager : IArtContextManager
    {
        Vector2 Pan { get; set; }

        Document Document { get; set; }
        IContainerLayer Root { get; set; }
        Matrix3x2 Transform { get; }
        float Zoom { get; set; }

        event PropertyChangedEventHandler DocumentUpdated;
        Vector2 FromArtSpace(Vector2 v);
        RectangleF FromArtSpace(RectangleF v);

        void Render(RenderContext target, ICacheManager cache);

        Vector2 ToArtSpace(Vector2 v);
        RectangleF ToArtSpace(RectangleF v);
    }
}