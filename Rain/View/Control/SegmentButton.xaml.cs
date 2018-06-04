using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;

namespace Rain.View.Control
{
    /// <summary>
    ///     Interaction logic for SegmentButton.xaml
    /// </summary>
    [ContentProperty("Segments")]
    public partial class SegmentButton
    {
        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register("SelectedValue",
                                        typeof(object),
                                        typeof(SegmentButton),
                                        new FrameworkPropertyMetadata(null, SelectedValueChanged));

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register("SelectedIndex",
                                        typeof(int),
                                        typeof(SegmentButton),
                                        new FrameworkPropertyMetadata(0,
                                                                      FrameworkPropertyMetadataOptions.AffectsRender,
                                                                      SelectedIndexChanged));

        public static readonly DependencyProperty SelectedSegmentProperty =
            DependencyProperty.Register("SelectedSegment",
                                        typeof(Segment),
                                        typeof(SegmentButton),
                                        new FrameworkPropertyMetadata(null, SelectedSegmentChanged));

        public static readonly DependencyProperty SegmentsProperty =
            DependencyProperty.Register("Segments",
                                        typeof(ObservableCollection<Segment>),
                                        typeof(SegmentButton),
                                        new PropertyMetadata(null, OnSegmentsChanged));

        private bool _selectionChanging;

        static SegmentButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SegmentButton),
                                                     new FrameworkPropertyMetadata(typeof(SegmentButton)));
        }

        public SegmentButton()
        {
            Segments = new ObservableCollection<Segment>();
            InitializeComponent();
        }

        public ObservableCollection<Segment> Segments
        {
            get => (ObservableCollection<Segment>) GetValue(SegmentsProperty);
            set => SetValue(SegmentsProperty, value);
        }

        public int SelectedIndex
        {
            get => (int) GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public Segment SelectedSegment
        {
            get => (Segment) GetValue(SelectedSegmentProperty);
            set => SetValue(SelectedSegmentProperty, value);
        }

        public object SelectedValue
        {
            get => GetValue(SelectedValueProperty);
            set => SetValue(SelectedValueProperty, value);
        }

        private static void OnSegmentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sb = (SegmentButton) d;

            if (e.OldValue is ObservableCollection<Segment> old)
                old.CollectionChanged -= sb.SegmentsChanged;

            if (e.NewValue is ObservableCollection<Segment> current)
                current.CollectionChanged += sb.SegmentsChanged;
        }

        private void SegmentOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            if (!(sender is Segment segment)) return;

            var index = Segments.IndexOf(segment);

            if (index == -1) return;

            SelectedIndex = index;
        }

        private void SegmentsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    foreach (Segment segment in e.OldItems) segment.Click -= SegmentOnClick;

                    break;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    foreach (Segment segment in e.NewItems) segment.Click += SegmentOnClick;

                    break;
            }
        }

        private static void SelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is SegmentButton segmentButton)) return;

            if (segmentButton._selectionChanging) return;

            segmentButton._selectionChanging = true;
            segmentButton.SelectedSegment = segmentButton.Segments.ElementAtOrDefault(segmentButton.SelectedIndex);
            segmentButton.SelectedValue = segmentButton.SelectedSegment?.Value;
            segmentButton._selectionChanging = false;
        }

        private static void SelectedSegmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is SegmentButton segmentButton)) return;

            foreach (var s in segmentButton.Segments)
                s.IsChecked = false;
            segmentButton.SelectedSegment.IsChecked = true;

            if (segmentButton._selectionChanging) return;

            segmentButton._selectionChanging = true;
            segmentButton.SelectedIndex = segmentButton.Segments.IndexOf(segmentButton.SelectedSegment);
            segmentButton.SelectedValue = segmentButton.SelectedSegment?.Value;
            segmentButton._selectionChanging = false;
        }

        private static void SelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is SegmentButton segmentButton)) return;

            if (segmentButton._selectionChanging) return;

            segmentButton._selectionChanging = true;
            var segment = segmentButton.Segments.FirstOrDefault(
                s => segmentButton.SelectedValue == s.Value ||
                     segmentButton.SelectedValue?.Equals(s.Value) == true);

            if (segment != null)
            {
                segmentButton.SelectedSegment = segment;
                segmentButton.SelectedIndex = segmentButton.Segments.IndexOf(segment);
            }

            segmentButton._selectionChanging = false;
        }
    }

    public class Segment : ToggleButton
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value",
                                        typeof(object),
                                        typeof(Segment),
                                        new FrameworkPropertyMetadata(default(object)));

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }
}