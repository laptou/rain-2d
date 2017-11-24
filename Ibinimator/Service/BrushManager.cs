﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core.Model;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.View.Control;

namespace Ibinimator.Service
{
    public class BrushManager : Core.Model.Model, IBrushManager
    {
        private bool _selecting;

        public BrushManager(IArtContext artContext, ISelectionManager selectionManager, IHistoryManager historyManager)
        {
            Context = artContext;

            selectionManager.Updated += OnUpdated;

            historyManager.Traversed += (s, e) => OnUpdated(s, null);

            Fill = new SolidColorBrushInfo(new Color(0, 0, 0));
            Stroke = new PenInfo
            {
                Brush = new SolidColorBrushInfo(new Color(0, 0, 0))
            };

            Stroke.PropertyChanged += StrokeOnPropertyChanged;

            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object o, PropertyChangedEventArgs args)
        {
            // otherwise, selecting new shapes applies their properties to all
            // of the other selected shapes
            if (_selecting) return;

            // lock b/c we can't be applying multiple changes at the same time
            lock (this)
            {
                switch (args.PropertyName)
                {
                    case nameof(Fill):
                        Context.ToolManager.Tool?.ApplyFill(Fill);
                        break;
                    case nameof(Stroke):
                        Stroke.PropertyChanged -= StrokeOnPropertyChanged;
                        Stroke.PropertyChanged += StrokeOnPropertyChanged;

                        Context.ToolManager.Tool?.ApplyStroke(Stroke);

                        break;
                }
            }
        }

        private void StrokeOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            Context.ToolManager.Tool?.ApplyStroke(Stroke);
            RaisePropertyChanged(nameof(Stroke));
        }

        private void OnUpdated(object sender, EventArgs args)
        {
            _selecting = true;
            var layer = Context.SelectionManager.Selection.LastOrDefault();

            if (layer is IFilledLayer filled)
                Fill = filled.Fill;

            if (layer is IStrokedLayer stroked)
            {
                Stroke = stroked.Stroke;
                Stroke.PropertyChanged -= StrokeOnPropertyChanged;
                Stroke.PropertyChanged += StrokeOnPropertyChanged;
            }

            _selecting = false;
        }

        #region IBrushManager Members

        public IArtContext Context { get; }

        public BrushInfo Fill
        {
            get => Get<BrushInfo>();
            set => Set(value);
        }

        public PenInfo Stroke
        {
            get => Get<PenInfo>();
            set => Set(value);
        }

        #endregion
    }
}