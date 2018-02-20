using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

using Rain.Core;

namespace Rain.Renderer
{
    public class HistoryManager : Core.Model.Model, IHistoryManager
    {
        private readonly Stack<IOperationCommand> _redo = new Stack<IOperationCommand>();
        private readonly Stack<IOperationCommand> _undo = new Stack<IOperationCommand>();

        public HistoryManager(IArtContext context) { Context = context; }

        public long NextId => Position + 1;

        #region IHistoryManager Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event EventHandler<long> Traversed;

        /// <inheritdoc />
        public void Attach(IArtContext context)
        {
            // HistoryManager doesn't subscribe to events from any other managers.
        }

        public void Clear()
        {
            lock (this)
            {
                _undo.Clear();
                _redo.Clear();
                Position = 0;
            }

            CollectionChanged?.Invoke(this,
                                      new NotifyCollectionChangedEventArgs(
                                          NotifyCollectionChangedAction.Reset));
        }

        /// <inheritdoc />
        public void Detach(IArtContext context)
        {
            // HistoryManager doesn't subscribe to events from any other managers.
        }

        public void Do(IOperationCommand command)
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

        public IEnumerator<IOperationCommand> GetEnumerator()
        {
            lock (this)
            {
                return _redo.Reverse().Concat(_undo).ToList().GetEnumerator();
            }
        }

        public void Merge(IOperationCommand command, long timeLimit)
        {
            lock (this)
            {
                _redo.Clear();
                var old = _undo.Count > 0 ? _undo.Peek() : null;

                if (old?.GetType() == command.GetType())
                {
                    command.Do(Context);

                    if (command.Time - old.Time < timeLimit &&
                        old.Merge(command) is IOperationCommand newCmd)
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

        public IOperationCommand Pop()
        {
            IOperationCommand result;

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

        public void Push(IOperationCommand command)
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

        public void Replace(IOperationCommand command)
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

        public IOperationCommand Current
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
                    while (value > Position &&
                           _redo.Count > 0)
                    {
                        var record = _redo.Pop();
                        record.Do(Context);
                        _undo.Push(record);
                    }

                    while (value < Position &&
                           _undo.Count > 0)
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
        #region IEqualityComparer<KeyValuePair<TK,TV>> Members

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