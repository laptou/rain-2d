﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Input;
using Ibinimator.Core.Model;

namespace Ibinimator.Core
{
    public interface ISelectionManager : IArtContextManager
    {
        IEnumerable<ILayer> Selection { get; }
        RectangleF SelectionBounds { get; }
        Matrix3x2 SelectionTransform { get; }

        event EventHandler Updated;
        void ClearSelection();

        Vector2 FromSelectionSpace(Vector2 v);
        Vector2 ToSelectionSpace(Vector2 v);

        void TransformSelection(Vector2 scale, Vector2 translate, float rotate, float shear, Vector2 origin);
        void UpdateBounds(bool reset);
    }
}