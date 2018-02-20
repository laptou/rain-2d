using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core;
using Rain.Core.Model;
using Rain.Core.Model.Paint;
using Rain.Core.Utility;

namespace Rain.ViewModel
{
    public class StrokeViewModel : ViewModel
    {
        public StrokeViewModel(IArtContext artContext) { Context = artContext; }

        public IArtContext Context
        {
            get => Get<IArtContext>();
            set => Set(value, OnContextChanging, OnContextChanged);
        }

        public IPenInfo Current
        {
            get
            {
                if (Context == null) return null;

                var res = Context.BrushManager.Query();

                return res.Stroke;
            }
        }


        public ObservableList<float> Dashes
        {
            get => Current?.Dashes;
            set
            {
                if (Current == null) return;

                Current.Dashes = value;
                Apply();
            }
        }

        public float DashOffset
        {
            get => Current?.DashOffset ?? 0;
            set
            {
                if (Current == null) return;

                Current.DashOffset = value;
                Apply();
            }
        }

        public bool HasDashes
        {
            get => Current?.HasDashes ?? false;
            set
            {
                if (Current == null) return;

                Current.HasDashes = value;
                Apply();
            }
        }

        public LineCap LineCap
        {
            get => Current?.LineCap ?? LineCap.Butt;
            set
            {
                if (Current == null) return;

                Current.LineCap = value;
                Apply();
            }
        }

        public LineJoin LineJoin
        {
            get => Current?.LineJoin ?? LineJoin.Miter;
            set
            {
                if (Current == null) return;

                Current.LineJoin = value;
                Apply();
            }
        }

        public float MiterLimit
        {
            get => Current?.MiterLimit ?? 0;
            set
            {
                if (Current == null) return;

                Current.MiterLimit = value;
                Apply();
            }
        }

        public float Width
        {
            get => Current?.Width ?? 0;
            set
            {
                if (Current == null) return;

                Current.Width = value;
                Apply();
            }
        }

        private void Apply() { Context.BrushManager.Apply(Current); }

        private void OnContextChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Context == null)
                return;

            Context.HistoryManager.CollectionChanged += OnHistoryChanged;
            Context.SelectionManager.SelectionChanged += OnSelectionChanged;
        }

        private void OnContextChanging(object sender, PropertyChangingEventArgs e)
        {
            if (Context == null)
                return;

            Context.HistoryManager.CollectionChanged -= OnHistoryChanged;
            Context.SelectionManager.SelectionChanged -= OnSelectionChanged;
        }

        private void OnHistoryChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Update();
        }

        private void OnSelectionChanged(object sender, EventArgs e) { Update(); }

        private void Update()
        {
            RaisePropertyChanged(nameof(Dashes),
                                 nameof(DashOffset),
                                 nameof(HasDashes),
                                 nameof(LineCap),
                                 nameof(LineJoin),
                                 nameof(MiterLimit),
                                 nameof(Width));
        }
    }
}