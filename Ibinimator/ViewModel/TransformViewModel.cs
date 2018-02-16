﻿using System;
using System.Collections.Generic;

using Ibinimator.Core.Utility;

using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Renderer;

namespace Ibinimator.ViewModel
{
    public class TransformViewModel : ViewModel
    {
        public TransformViewModel(IArtContext artContext)
        {
            ArtContext = artContext;
            artContext.SelectionManager.SelectionChanged += (s, e) =>
                                                            {
                                                                RaisePropertyChanged(nameof(X));
                                                                RaisePropertyChanged(nameof(Y));
                                                                RaisePropertyChanged(nameof(Width));
                                                                RaisePropertyChanged(
                                                                    nameof(Height));
                                                                RaisePropertyChanged(
                                                                    nameof(Rotation));
                                                                RaisePropertyChanged(nameof(Shear));
                                                            };
        }

        public IArtContext ArtContext { get; }

        public float Height
        {
            get => GetSize().Y;
            set => SetSize(new Vector2(Width, value));
        }

        public SelectionHandle Origin
        {
            get => Get<SelectionHandle>();
            set => Set(value);
        }

        public float Rotation
        {
            get => ArtContext.SelectionManager?.SelectionTransform.GetRotation() ?? 0;
            set => ArtContext.SelectionManager?.TransformSelection(
                Vector2.One,
                Vector2.Zero,
                value - Rotation,
                0,
                Vector2.One * 0.5f);
        }

        public float Shear
        {
            get => ArtContext.SelectionManager?.SelectionTransform.GetShear() ?? 0;
            set => ArtContext.SelectionManager?.TransformSelection(
                Vector2.One,
                Vector2.Zero,
                0,
                value - Shear,
                Vector2.Zero);
        }

        public float Width
        {
            get => GetSize().X;
            set => SetSize(new Vector2(value, Height));
        }

        public float X
        {
            get => GetPosition().X;
            set => SetPosition(new Vector2(value, Y));
        }

        public float Y
        {
            get => GetPosition().Y;
            set => SetPosition(new Vector2(X, value));
        }

        private Vector2 GetPosition()
        {
            return Vector2.Transform(ArtContext.SelectionManager.SelectionBounds.TopLeft,
                                     ArtContext.SelectionManager.SelectionTransform);
        }

        private Vector2 GetSize()
        {
            if (ArtContext.SelectionManager == null)
                return Vector2.Zero;

            return ArtContext.SelectionManager.SelectionBounds.Size *
                   ArtContext.SelectionManager.SelectionTransform.GetScale();
        }

        private void SetPosition(Vector2 position)
        {
            ArtContext.SelectionManager?.TransformSelection(Vector2.One,
                                                            position - GetPosition(),
                                                            0,
                                                            0,
                                                            Vector2.Zero);
        }

        private void SetSize(Vector2 size)
        {
            ArtContext.SelectionManager?.TransformSelection(size / GetSize(),
                                                            Vector2.Zero,
                                                            0,
                                                            0,
                                                            Vector2.Zero);
        }
    }
}