using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.Service.Commands;
using Ibinimator.View.Control;

namespace Ibinimator.Service
{
    public class HistoryManager : Model.Model, IHistoryManager
    {
        private readonly Stack<IOperationCommand<ILayer>> _redo = new Stack<IOperationCommand<ILayer>>();
        private readonly Stack<IOperationCommand<ILayer>> _undo = new Stack<IOperationCommand<ILayer>>();

        public HistoryManager(ArtView artView)
        {
            ArtView = artView;
        }

        public long NextId => Time + 1;

        #region IHistoryManager Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event EventHandler<long> Traversed;

        public void Clear()
        {
            lock (this)
            {
                _undo.Clear();
                _redo.Clear();
                Time = 0;
            }

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Do(IOperationCommand<ILayer> command)
        {
            command.Do(ArtView);
            Push(command);
        }

        public IEnumerator<IOperationCommand<ILayer>> GetEnumerator()
        {
            lock (this)
            {
                return _redo.Reverse().Concat(_undo).ToList().GetEnumerator();
            }
        }

        public IOperationCommand<ILayer> Pop()
        {
            IOperationCommand<ILayer> result;

            lock (this)
            {
                _redo.Clear();
                result = _undo.Pop();

                RaisePropertyChanged(nameof(Current));
                RaisePropertyChanged(nameof(NextId));
                RaisePropertyChanged(nameof(Time));

                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            return result;
        }

        public void Push(IOperationCommand<ILayer> command)
        {
            lock (this)
            {
                _redo.Clear();
                _undo.Push(command);

                RaisePropertyChanged(nameof(Current));
                RaisePropertyChanged(nameof(NextId));
                RaisePropertyChanged(nameof(Time));

                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public void Redo()
        {
            Time++;
        }

        public void Replace(IOperationCommand<ILayer> command)
        {
            lock (this)
            {
                _redo.Clear();
                _undo.Pop();
                _undo.Push(command);

                RaisePropertyChanged(nameof(Current));
                RaisePropertyChanged(nameof(NextId));
                RaisePropertyChanged(nameof(Time));

                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public void Undo()
        {
            Time--;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ArtView ArtView { get; }

        public IOperationCommand<ILayer> Current
        {
            get
            {
                lock (this)
                    return _undo.Count > 0 ? _undo.Peek() : null;
            }
        }

        public long Time
        {
            get => Current?.Id ?? 0;
            set
            {
                lock (this)
                {
                    while (value > Time && _redo.Count > 0)
                    {
                        var record = _redo.Pop();
                        record.Do(ArtView);
                        _undo.Push(record);
                    }

                    while (value < Time && _undo.Count > 0)
                    {
                        var record = _undo.Pop();
                        record.Undo(ArtView);
                        _redo.Push(record);
                    }

                    RaisePropertyChanged(nameof(Current));
                    RaisePropertyChanged(nameof(NextId));
                    RaisePropertyChanged(nameof(Time));
                    Traversed?.Invoke(this, value);
                }
            }
        }

        #endregion
    }

    public class KeyComparer<K, V> : IEqualityComparer<KeyValuePair<K, V>>
    {
        #region IEqualityComparer<KeyValuePair<K,V>> Members

        public bool Equals(KeyValuePair<K, V> x, KeyValuePair<K, V> y)
        {
            return EqualityComparer<K>.Default.Equals(x.Key, y.Key);
        }

        public int GetHashCode(KeyValuePair<K, V> obj)
        {
            return EqualityComparer<K>.Default.GetHashCode(obj.Key);
        }

        #endregion
    }
}