using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Ibinimator.Shared
{
    public class ObservableList<T> : ObservableCollection<T>
    {
        private readonly object _locker = new object();

        /// <summary>
        ///     This private variable holds the flag to
        ///     turn on and off the collection changed notification.
        /// </summary>
        private bool _suspendCollectionChangeNotification;

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the FastObservableCollection class.
        /// </summary>
        public ObservableList()
        {
            _suspendCollectionChangeNotification = false;
        }

        public ObservableList(IEnumerable<T> items) : base(items)
        {
            _suspendCollectionChangeNotification = false;
        }

        /// <summary>
        ///     This event is overriden CollectionChanged event of the observable collection.
        /// </summary>
        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        ///     This method adds the given generic list of items
        ///     as a range into current collection by casting them as type T.
        ///     It then notifies once after all items are added.
        /// </summary>
        /// <param name="items">The source collection.</param>
        public void AddItems(IList<T> items)
        {
            lock (_locker)
            {
                SuspendCollectionChangeNotification();
                foreach (var i in items)
                    InsertItem(Count, i);
                NotifyChanges();
            }
        }

        /// <summary>
        ///     Raises collection change event.
        /// </summary>
        public void NotifyChanges()
        {
            ResumeCollectionChangeNotification();
            var arg
                = new NotifyCollectionChangedEventArgs
                    (NotifyCollectionChangedAction.Reset);
            OnCollectionChanged(arg);
        }

        /// <summary>
        ///     This method removes the given generic list of items as a range
        ///     into current collection by casting them as type T.
        ///     It then notifies once after all items are removed.
        /// </summary>
        /// <param name="items">The source collection.</param>
        public void RemoveItems(IList<T> items)
        {
            lock (_locker)
            {
                SuspendCollectionChangeNotification();
                foreach (var i in items)
                    Remove(i);
                NotifyChanges();
            }
        }

        /// <summary>
        ///     Resumes collection changed notification.
        /// </summary>
        public void ResumeCollectionChangeNotification()
        {
            _suspendCollectionChangeNotification = false;
        }

        /// <summary>
        ///     Suspends collection changed notification.
        /// </summary>
        public void SuspendCollectionChangeNotification()
        {
            _suspendCollectionChangeNotification = true;
        }

        /// <inheritdoc />
        /// <summary>
        ///     This collection changed event performs thread safe event raising.
        /// </summary>
        /// <param name="e">The event argument.</param>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // Recommended is to avoid reentry 
            // in collection changed event while collection
            // is getting changed on other thread.
            using (BlockReentrancy())
            {
                if (_suspendCollectionChangeNotification) return;

                var eventHandler = CollectionChanged;

                if (eventHandler == null)
                    return;

                // Walk thru invocation list.
                var delegates = eventHandler.GetInvocationList();

                foreach (var @delegate in delegates)
                {
                    var handler = (NotifyCollectionChangedEventHandler) @delegate;
                    // If the subscriber is a DispatcherObject and different thread.

                    if (handler.Target is DispatcherObject dispatcherObject
                        && !dispatcherObject.CheckAccess())
                        dispatcherObject.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, handler, this, e);
                    else
                        handler(this, e);
                }
            }
        }
    }
}