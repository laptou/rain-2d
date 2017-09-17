using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Ibinimator.Shared;
using System.Linq;
using System.Threading.Tasks;
using FastMember;
using Ibinimator.Model;
using Ibinimator.Service.Commands;
using Ibinimator.View.Control;

namespace Ibinimator.Service
{
    public class Watcher<TK, T> : IDisposable where T : INotifyPropertyChanging, INotifyPropertyChanged
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly List<string> Animatable;

        // ReSharper disable once StaticMemberInGenericType
        private static readonly List<string> Undoable;

        // ReSharper disable once StaticMemberInGenericType
        private static readonly List<string> Observable;

        private readonly Func<T, TK> _keySelector;

        private readonly Dictionary<(TK id, string property), object> _newProperties =
            new Dictionary<(TK, string), object>();

        private readonly Dictionary<(TK id, string property), object> _oldProperties =
            new Dictionary<(TK, string), object>();

        private readonly Dictionary<Type, TypeAccessor> _accessors =
            new Dictionary<Type, TypeAccessor> ();

        private List<string> _animatable;
        private List<string> _undoable;
        private List<string> _observable;

        static Watcher()
        {
            var props = typeof(T).GetProperties();

            Animatable =
            (from p in props
                where p.GetCustomAttributes(typeof(AnimatableAttribute), false).Any()
                select p.Name).ToList();

            Undoable =
            (from p in props
                where p.GetCustomAttributes(typeof(UndoableAttribute), false).Any()
                select p.Name).ToList();

            Observable =
            (from p in props
                where typeof(INotifyCollectionChanged).IsAssignableFrom(p.PropertyType)
                select p.Name).ToList();
        }

        public Watcher(T target, Func<T, TK> keySelector)
        {
            _keySelector = keySelector;

            Target = target;

            var type = target.GetType();

            Check(type);

            var collections =
                Enumerable.Union(
                    RecordAnimatable ? _animatable.AsEnumerable() : new string[0],
                    RecordUndoable ? _undoable.AsEnumerable() : new string[0]);

            collections = collections.Intersect(_observable);

            foreach (var collection in collections)
                _oldProperties[(_keySelector(target), collection)] =
                    Duplicate(_accessors[type][target, collection]);

            Target.PropertyChanging += TargetOnPropertyChanging;
            Target.PropertyChanged += TargetOnPropertyChanged;
        }

        public IDictionary<(TK id, string property), object> NewProperties => _newProperties;
        public IDictionary<(TK id, string property), object> OldProperties => _oldProperties;
        public bool RecordAnimatable { get; set; } = false;
        public bool RecordUndoable { get; set; } = true;

        public T Target { get; }

        #region IDisposable Members

        public void Dispose()
        {
            _newProperties.Clear();
            _oldProperties.Clear();
            Target.PropertyChanging -= TargetOnPropertyChanging;
            Target.PropertyChanged -= TargetOnPropertyChanged;
        }

        #endregion

        private void Check(Type type)
        {
            if (_accessors.ContainsKey(type)) return;

            // must be a subclass of T, get additional properties

            _accessors.Add(type, TypeAccessor.Create(type));

            var props = type.GetProperties();

            _animatable =
                Animatable.Union(
                    from p in props
                    where p.GetCustomAttributes(typeof(AnimatableAttribute), false).Any()
                    select p.Name).ToList();

            _undoable =
                Undoable.Union(
                    from p in props
                    where p.GetCustomAttributes(typeof(UndoableAttribute), false).Any()
                    select p.Name).ToList();

            _observable =
                Observable.Union(
                    from p in props
                    where typeof(INotifyCollectionChanged).IsAssignableFrom(p.PropertyType)
                    select p.Name).ToList();
        }

        private static object Duplicate(object obj)
        {
            switch (obj)
            {
                case IDisposable _:
                    throw new InvalidOperationException("Disposable objects should not be cloned.");
                case ValueType _:
                    return obj;
                case ICloneable cloneable:
                    return cloneable.Clone();
                case IList list:
                    var newList = (IList) Activator.CreateInstance(obj.GetType());

                    foreach (var o in list)
                        newList.Add(Duplicate(o));

                    return newList;
            }

            return obj;
        }

        private void TargetOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var type = sender.GetType();
            Check(type);

            if (RecordAnimatable && _animatable.Contains(e.PropertyName) ||
                RecordUndoable && _undoable.Contains(e.PropertyName))
                _newProperties[(_keySelector((T) sender), e.PropertyName)] =
                    Duplicate(_accessors[type][sender, e.PropertyName]);
        }

        private void TargetOnPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            var type = sender.GetType();
            Check(type);

            if (RecordAnimatable && _animatable.Contains(e.PropertyName) ||
                RecordUndoable && _undoable.Contains(e.PropertyName))
            {
                var key = (_keySelector((T) sender), e.PropertyName);
                if (!_oldProperties.ContainsKey(key))
                    _oldProperties[key] =
                        Duplicate(_accessors[type][sender, e.PropertyName]);
            }
        }
    }

    public class HistoryManager : Model.Model, IHistoryManager
    {
        private readonly Stack<IOperationCommand<ILayer>> _redo = new Stack<IOperationCommand<ILayer>>();
        private readonly Stack<IOperationCommand<ILayer>> _undo = new Stack<IOperationCommand<ILayer>>();

        public HistoryManager(ArtView artView)
        {
            ArtView = artView;
        }

        #region IHistoryManager Members

        public ArtView ArtView { get; }

        public long NextId => Time + 1;

        public void Push(IOperationCommand<ILayer> command)
        {
            _redo.Clear();
            _undo.Push(command);

            RaisePropertyChanged(nameof(Current));
            RaisePropertyChanged(nameof(NextId));
            RaisePropertyChanged(nameof(Time));

            TimeChanged?.Invoke(this, Time);
            CollectionChanged?.Invoke(this, 
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void Do(IOperationCommand<ILayer> command)
        {
            command.Do();
            Push(command);
        }

        public IOperationCommand<ILayer> Pop()
        {
            _redo.Clear();
            var result = _undo.Pop();

            RaisePropertyChanged(nameof(Current));
            RaisePropertyChanged(nameof(NextId));
            RaisePropertyChanged(nameof(Time));

            TimeChanged?.Invoke(this, Time);
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            return result;
        }

        public void Redo()
        {
            Time++;
        }

        public void Undo()
        {
            Time--;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
            Time = 0;

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public IOperationCommand<ILayer> Current => _undo.Count > 0 ? _undo.Peek() : null;

        public long Time
        {
            get => Current?.Id ?? 0;
            set
            {
                while (value > Time && _redo.Count > 0)
                {
                    var record = _redo.Pop();
                    record.Do();
                    _undo.Push(record);
                }

                while (value < Time && _undo.Count > 0)
                {
                    var record = _undo.Pop();
                    record.Undo();
                    _redo.Push(record);
                }

                RaisePropertyChanged(nameof(Current));
                RaisePropertyChanged(nameof(NextId));
                RaisePropertyChanged(nameof(Time));
                TimeChanged?.BeginInvoke(this, value, null, null);
            }
        }

        #endregion

        public event EventHandler<long> TimeChanged;

        public IEnumerator<IOperationCommand<ILayer>> GetEnumerator()
        {
            return _redo.Reverse().Concat(_undo).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
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