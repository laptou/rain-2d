using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Ibinimator.Core;
using Ibinimator.Core.Model;
using Ibinimator.Core.Utility;
using Ibinimator.Service.Commands;

namespace Ibinimator.Service
{
    public enum SelectionHandle
    {
        Scale       = 1 << -3,
        Rotation    = 1 << -2,
        Translation = 1 << -1,
        Top         = (1 << 0) | Scale,
        Left        = (1 << 1) | Scale,
        Right       = (1 << 2) | Scale,
        Bottom      = (1 << 3) | Scale,
        TopLeft     = Top | Left,
        TopRight    = Top | Right,
        BottomLeft  = Bottom | Left,
        BottomRight = Bottom | Right
    }

    public sealed class SelectionManager : Core.Model.Model, ISelectionManager
    {
        private RectangleF _selectionBounds;
        private Matrix3x2  _selectionTransform = Matrix3x2.Identity;


        public SelectionManager(
            IArtContext     artView,
            IViewManager    viewManager,
            IHistoryManager historyManager,
            ICacheManager   cacheManager)
        {
            Context = artView;

            Selection = new ObservableList<ILayer>();
            Selection.CollectionChanged += (sender, args) =>
                                           {
                                               UpdateBounds(true);
                                               Updated?.Invoke(this, null);
                                           };

            viewManager.DocumentUpdated += (sender, args) =>
                                           {
                                               if (args.PropertyName != nameof(ILayer.Selected)) return;

                                               var layer = (ILayer) sender;

                                               var contains = Selection.Contains(layer);

                                               if (layer.Selected && !contains)
                                               {
                                                   Selection.Add(layer);

                                                   foreach (var child in layer.Flatten().Skip(1))
                                                       child.Selected = false;
                                               }
                                               else if (!layer.Selected && contains)
                                               {
                                                   Selection.Remove(layer);
                                               }
                                           };

            historyManager.Traversed += (sender, args) => { UpdateBounds(true); };

            cacheManager.BoundsChanged += (sender, args) => { UpdateBounds(true); };
        }

        public ObservableList<ILayer> Selection { get; }

        #region ISelectionManager Members

        public event EventHandler Updated;

        public void ClearSelection()
        {
            while (Selection.Count > 0) Selection[0].Selected = false;
        }

        public Vector2 FromSelectionSpace(Vector2 v) { return Vector2.Transform(v, SelectionTransform); }

        public Vector2 ToSelectionSpace(Vector2 v)
        {
            return Vector2.Transform(v, MathUtils.Invert(SelectionTransform));
        }

        public void TransformSelection(
            Vector2 scale,
            Vector2 translate,
            float   rotate,
            float   shear,
            Vector2 relativeOrigin)
        {
            var localOrigin = SelectionBounds.TopLeft +
                              SelectionBounds.Size * relativeOrigin;
            var origin = FromSelectionSpace(localOrigin);

            // order doesn't really matter since only one of 
            // these will be non-default at a time

            var transform =
                MathUtils.Invert(SelectionTransform) *
                Matrix3x2.CreateScale(scale, localOrigin) *
                Matrix3x2.CreateSkew(shear, 0, localOrigin) *
                SelectionTransform *
                Matrix3x2.CreateTranslation(-origin) *
                Matrix3x2.CreateRotation(rotate) *
                Matrix3x2.CreateTranslation(translate) *
                Matrix3x2.CreateTranslation(origin);

            SelectionTransform = SelectionTransform *
                                 transform;

            if (transform.IsIdentity) return;

            var command = new TransformCommand(
                Context.HistoryManager.Position + 1,
                Selection.ToArray(),
                global: transform);

            Context.HistoryManager.Merge(command, 500);

            Context.InvalidateSurface();

            Updated?.Invoke(this, null);
        }

        public void UpdateBounds(bool reset)
        {
            if (Selection.Count == 0)
            {
                SelectionBounds = RectangleF.Empty;
                SelectionTransform = Matrix3x2.Identity;
            }

            if (Selection.Count == 1)
            {
                var layer = Selection[0];

                SelectionBounds = Context.CacheManager.GetBounds(layer);
                SelectionTransform = layer.AbsoluteTransform;
            }

            if (Selection.Count > 1)
            {
                SelectionBounds = Selection.Select(Context.CacheManager.GetAbsoluteBounds)
                                           .Aggregate(RectangleF.Union);
                SelectionTransform = Matrix3x2.Identity;
            }

            Context.InvalidateSurface();

            Updated?.Invoke(this, null);
        }

        public IArtContext Context { get; }

        public RectangleF SelectionBounds
        {
            get => _selectionBounds;
            private set
            {
                _selectionBounds = value;
                RaisePropertyChanged(nameof(SelectionBounds));
            }
        }

        public Matrix3x2 SelectionTransform
        {
            get => _selectionTransform;
            private set
            {
                _selectionTransform = value;
                RaisePropertyChanged(nameof(SelectionTransform));
            }
        }

        IEnumerable<ILayer> ISelectionManager.Selection => Selection;

        #endregion
    }

    public enum GuideType
    {
        All        = 0,
        Linear     = 1 << -1,
        Radial     = 1 << -2,
        Proportion = (1 << 0) | Linear,
        Position   = (1 << 1) | Linear,
        Rotation   = (1 << 2) | Radial
    }
}