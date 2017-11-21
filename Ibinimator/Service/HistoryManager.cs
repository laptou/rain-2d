using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.View.Control;

namespace Ibinimator.Service
{
    public class HistoryManager : Core.Model.Model, IHistoryManager
    {
        private readonly Stack<IOperationCommand<ILayer>> _redo = new Stack<IOperationCommand<ILayer>>();
        private readonly Stack<IOperationCommand<ILayer>> _undo = new Stack<IOperationCommand<ILayer>>();

        public HistoryManager(IArtContext context) { Context = context; }

        public long NextId => Position + 1;

        #region IHistoryManager Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event EventHandler<long> Traversed;

        public void Clear()
        {
            lock (this)
            {
                _undo.Clear();
                _redo.Clear();
                Position = 0;
            }

            CollectionChanged?.Invoke(
                this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Do(IOperationCommand<ILayer> command)
        {
            try
            {
                command.Do(Context);
                Push(command);
            }
            catch (Exception e)
            {
                Context.Status = new Status(Status.StatusType.Error, e.Message);
            }
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
                RaisePropertyChanged(nameof(Position));

                CollectionChanged?.Invoke(this,
                                          new NotifyCollectionChangedEventArgs(
                                              NotifyCollectionChangedAction.Reset));
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
                RaisePropertyChanged(nameof(Position));

                CollectionChanged?.Invoke(this,
                                          new NotifyCollectionChangedEventArgs(
                                              NotifyCollectionChangedAction.Reset));
            }
        }

        public void Redo() { Position++; }

        public void Replace(IOperationCommand<ILayer> command)
        {
            lock (this)
            {
                _redo.Clear();
                _undo.Pop();
                _undo.Push(command);

                RaisePropertyChanged(nameof(Current));
                RaisePropertyChanged(nameof(NextId));
                RaisePropertyChanged(nameof(Position));

                CollectionChanged?.Invoke(this,
                                          new NotifyCollectionChangedEventArgs(
                                              NotifyCollectionChangedAction.Reset));
            }
        }

        public void Undo() { Position--; }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        public IArtContext Context { get; }

        public IOperationCommand<ILayer> Current
        {
            get
            {
                lock (this)
                {
                    return _undo.Count > 0 ? _undo.Peek() : null;
                }
            }
        }

        public long Position
        {
            get => Current?.Id ?? 0;
            set
            {
                lock (this)
                {
                    while (value > Position && _redo.Count > 0)
                    {
                        var record = _redo.Pop();
                        record.Do(Context);
                        _undo.Push(record);
                    }

                    while (value < Position && _undo.Count > 0)
                    {
                        var record = _undo.Pop();
                        record.Undo(Context);
                        _redo.Push(record);
                    }

                    RaisePropertyChanged(nameof(Current));
                    RaisePropertyChanged(nameof(NextId));
                    RaisePropertyChanged(nameof(Position));
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