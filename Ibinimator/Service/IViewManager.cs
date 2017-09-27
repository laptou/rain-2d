﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using SharpDX;
using SharpDX.Direct2D1;

namespace Ibinimator.Service
{
    public interface IViewManager : IArtViewManager
    {
        Document Document { get; set; }
        Vector2 Pan { get; set; }

        Group Root { get; set; }
        Matrix3x2 Transform { get; }
        float Zoom { get; set; }

        event PropertyChangedEventHandler DocumentUpdated;
        Vector2 FromArtSpace(Vector2 v);
        RectangleF FromArtSpace(RectangleF v);

        Vector2 ToArtSpace(Vector2 v);
        RectangleF ToArtSpace(RectangleF v);

        void Render(RenderTarget target, ICacheManager cache);
    }
}