using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Core.Model;

namespace Ibinimator.Service
{
    public class HistoryManager : Model, IHistoryManager
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

        public void Merge(IOperationCommand<ILayer> command, long timeLimit)
        {
            lock (this)
            {
                _redo.Clear();
                var old = _undo.Count > 0 ? _undo.Peek() : null;

                if (old?.GetType() == command.GetType())
                {
                    command.Do(Context);

                    if (command.Time - old.Time < timeLimit &&
                        old.Merge(command) is IOperationCommand<ILayer> newCmd)
                        Replace(newCmd);
                    else Push(command);
                }
                else
                {
                    Do(command);
                    CollectionChanged?.Invoke(this,
                                              new NotifyCollectionChangedEventArgs(
                                                  NotifyCollectionChangedAction.Reset));
                }
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

                CollectionChanged?.Invoke(this,
                                          new NotifyCollectionChangedEventArgs(
                                              NotifyCollectionChangedAction.Reset));

                RaisePropertyChanged(nameof(Current));
                RaisePropertyChanged(nameof(NextId));
                RaisePropertyChanged(nameof(Position));
            }

            return result;
        }

        public void Push(IOperationCommand<ILayer> command)
        {
            lock (this)
            {
                _redo.Clear();
                _undo.Push(command);

                CollectionChanged?.Invoke(this,
                                          new NotifyCollectionChangedEventArgs(
                                              NotifyCollectionChangedAction.Reset));

                RaisePropertyChanged(nameof(Current));
                RaisePropertyChanged(nameof(NextId));
                RaisePropertyChanged(nameof(Position));
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

                CollectionChanged?.Invoke(this,
                                          new NotifyCollectionChangedEventArgs(
                                              NotifyCollectionChangedAction.Reset));

                RaisePropertyChanged(nameof(Current));
                RaisePropertyChanged(nameof(NextId));
                RaisePropertyChanged(nameof(Position));
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

    public class KeyComparer<TK, TV> : IEqualityComparer<KeyValuePair<TK, TV>>
    {
        #region IEqualityComparer<KeyValuePair<K,V>> Members

        public bool Equals(KeyValuePair<TK, TV> x, KeyValuePair<TK, TV> y)
        {
            return EqualityComparer<TK>.Default.Equals(x.Key, y.Key);
        }

        public int GetHashCode(KeyValuePair<TK, TV> obj)
        {
            return EqualityComparer<TK>.Default.GetHashCode(obj.Key);
        }

        #endregion
    }
}