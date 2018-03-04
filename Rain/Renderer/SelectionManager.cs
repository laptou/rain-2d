using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Rain.Commands;
using Rain.Core;
using Rain.Core.Model;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Utility;

namespace Rain.Renderer
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

        public SelectionManager(IArtContext artView)
        {
            Context = artView;

            Selection = new ObservableList<ILayer>();
            Selection.CollectionChanged += (sender, args) =>
                                           {
                                               SelectionChanged?.Invoke(this, null);
                                               UpdateBounds();
                                           };
        }

        public ObservableList<ILayer> Selection { get; }

        public RectangleF SelectionBounds
        {
            get => _selectionBounds;
            private set
            {
                _selectionBounds = value;
                RaisePropertyChanged(nameof(SelectionBounds));
            }
        }

        private void OnBoundsChanged(object sender, EventArgs e) { UpdateBounds(); }

        private void OnDocumentUpdated(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ILayer layer)
            {
                switch (e.PropertyName)
                {
                    case nameof(ILayer.Selected):

                        var contains = Selection.Contains(layer);

                        if (layer.Selected &&
                            !contains)
                        {
                            Selection.Add(layer);

                            foreach (var child in layer.Flatten().Skip(1))
                                child.Selected = false;
                        }
                        else if (!layer.Selected && contains)
                        {
                            Selection.Remove(layer);
                        }

                        break;
                }
            }
        }

        private void OnHistoryTraversed(object sender, long e) { UpdateBounds(); }

        private void OnManagerAttached(object sender, EventArgs e)
        {
            if (sender is IViewManager view)
            {
                view.Document.Root.BoundsChanged += OnBoundsChanged;
                view.DocumentUpdated += OnDocumentUpdated;
            }
        }

        private void OnManagerDetached(object sender, EventArgs e)
        {
            if (sender is IViewManager view)
            {
                Selection.Clear();

                view.Document.Root.BoundsChanged -= OnBoundsChanged;
                view.DocumentUpdated -= OnDocumentUpdated;
            }
        }

        #region ISelectionManager Members

        /// <inheritdoc />
        public event EventHandler SelectionBoundsChanged;

        public event EventHandler SelectionChanged;

        /// <inheritdoc />
        public void Attach(IArtContext context)
        {
            context.ManagerAttached += OnManagerAttached;
            context.ManagerDetached += OnManagerDetached;

            if (context.ViewManager != null)
            {
                context.ViewManager.DocumentUpdated += OnDocumentUpdated;

                if (context.ViewManager.Document?.Root != null)
                    context.ViewManager.Document.Root.BoundsChanged += OnBoundsChanged;
            }

            context.HistoryManager.Traversed += OnHistoryTraversed;

            context.RaiseAttached(this);
        }

        public void ClearSelection()
        {
            while (Selection.Count > 0) Selection[0].Selected = false;
        }

        /// <inheritdoc />
        public void Detach(IArtContext context)
        {
            context.ManagerAttached -= OnManagerAttached;
            context.ManagerDetached -= OnManagerDetached;

            if (context.ViewManager.Document?.Root != null)
                context.ViewManager.Document.Root.BoundsChanged -= OnBoundsChanged;

            context.ViewManager.DocumentUpdated -= OnDocumentUpdated;
            context.HistoryManager.Traversed -= OnHistoryTraversed;

            context.RaiseDetached(this);
        }

        public Vector2 FromSelectionSpace(Vector2 v)
        {
            return Vector2.Transform(v, SelectionTransform);
        }

        public Vector2 ToSelectionSpace(Vector2 v)
        {
            return Vector2.Transform(v, MathUtils.Invert(SelectionTransform));
        }

        public void TransformSelection(
            Vector2 scale, Vector2 translate, float rotate, float shear, Vector2 relativeOrigin)
        {
            var localOrigin = SelectionBounds.TopLeft + SelectionBounds.Size * relativeOrigin;
            var origin = FromSelectionSpace(localOrigin);

            // order doesn't really matter since only one of 
            // these will be non-default at a time

            var transform = MathUtils.Invert(SelectionTransform) *
                            Matrix3x2.CreateScale(scale, localOrigin) *
                            Matrix3x2.CreateSkew(shear, 0, localOrigin) * SelectionTransform *
                            Matrix3x2.CreateTranslation(-origin) *
                            Matrix3x2.CreateRotation(rotate) *
                            Matrix3x2.CreateTranslation(translate) *
                            Matrix3x2.CreateTranslation(origin);

            SelectionTransform = SelectionTransform * transform;

            if (transform.IsIdentity) return;

            var targets = Selection.Where(l => !(l is Clone c && c.Target.Selected));

            var command = new TransformCommand(Context.HistoryManager.Position + 1,
                                               targets.ToArray(),
                                               global: transform);

            Context.HistoryManager.Merge(command, 500);

            Context.InvalidateRender();

            SelectionBoundsChanged?.Invoke(this, null);
        }

        public void UpdateBounds()
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

            Context.InvalidateRender();

            SelectionBoundsChanged?.Invoke(this, null);
        }

        public IArtContext Context { get; }

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