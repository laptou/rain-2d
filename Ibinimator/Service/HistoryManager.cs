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
        private readonly Stack<IRecord<long>> _redo = new Stack<IRecord<long>>();
        private readonly Stack<IRecord<long>> _undo = new Stack<IRecord<long>>();
        private Watcher<int, Document> _documentWatcher;
        private Watcher<Guid, Layer> _layerWatcher;

        public HistoryManager(ArtView artView)
        {
            ArtView = artView;
        }

        #region IHistoryManager Members

        public ArtView ArtView { get; }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<IRecord<long>> IEnumerable<IRecord<long>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Merge()
        {
            if (_layerWatcher != null || _documentWatcher != null)
                throw new InvalidOperationException("A record is currently being recorded.");

            lock (this)
            {
                var r1 = _undo.Pop();
                var r2 = _undo.Pop();

                var removed = _redo.Reverse().ToList();
                removed.Add(r1);
                removed.Add(r2);

                _redo.Clear();

                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        removed, 0));

                if (r1 is DocumentRecord dr1 && r2 is DocumentRecord dr2)
                {
                    if (dr1.Target != dr2.Target)
                        throw new Exception("The records do not have the same target.");

                    var kc = new KeyComparer<string, object>();

                    var dr3 = new DocumentRecord(
                        NextId,
                        dr2.Target,
                        dr2.OldProperties.Union(dr1.OldProperties, kc).ToDictionary(),
                        dr2.NewProperties.Union(dr1.NewProperties, kc).ToDictionary(),
                        dr2.Description);

                    _undo.Push(dr3);

                    CollectionChanged?.Invoke(
                        this,
                        new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Add,
                            dr3, 0));
                }
                else if (r1 is LayerRecord lr1 && r2 is LayerRecord lr2)
                {
                    if (!lr1.Targets.SequenceEqual(lr2.Targets))
                        throw new Exception("The records do not have the same target.");

                    var kc = new KeyComparer<(Guid, string), object>();

                    var lr3 = new LayerRecord(
                        NextId,
                        lr2.Targets,
                        lr2.OldProperties.Union(lr1.OldProperties, kc).ToDictionary(),
                        lr2.NewProperties.Union(lr1.NewProperties, kc).ToDictionary(),
                        lr2.Description);

                    _undo.Push(lr3);

                    CollectionChanged?.Invoke(
                        this,
                        new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Add,
                            lr3, 0));
                }
                else
                {
                    throw new InvalidOperationException("These two records cannot be merged.");
                }
            }
        }

        public long NextId => Time + 1;

        public void Redo()
        {
            Time++;
        }

        public void Undo()
        {
            Time--;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void BeginRecord()
        {
            var sf = new StackFrame(1).GetMethod();

            Trace.WriteLine($"BeginRecord: {sf.ReflectedType} {sf}");

            lock (this)
            {
                if (_layerWatcher != null || _documentWatcher != null)
                    throw new InvalidOperationException("A record is already being recorded.");

                _layerWatcher = new Watcher<Guid, Layer>(ArtView.ViewManager.Root, l => l.Id);
                _documentWatcher = new Watcher<int, Document>(ArtView.ViewManager.Document, d => d.GetHashCode());
            }
        }

        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
            Time = 0;
        }

        public IRecord<long> CurrentRecord => _undo.Count > 0 ? _undo.Peek() : null;

        public void EndRecord(string desc = null)
        {
            var sf = new StackFrame(1).GetMethod();

            Trace.WriteLine($"EndRecord: {sf.ReflectedType} {sf}");

            if (_layerWatcher == null) return;

            lock (this)
            {
                IRecord<long> record = null;

                if (_layerWatcher.NewProperties.Count > 0)
                {
                    var ids = _layerWatcher.NewProperties.Keys.Select(k => k.id).Distinct().ToArray();

                    record = new LayerRecord(
                        NextId,
                        ids,
                        _layerWatcher.OldProperties.ToDictionary(k => k.Key, k => k.Value),
                        _layerWatcher.NewProperties.ToDictionary(k => k.Key, k => k.Value),
                        desc);
                }

                _layerWatcher.Dispose();
                _layerWatcher = null;

                if (_documentWatcher.NewProperties.Count > 0)
                {
                    if (record != null) Debugger.Break();

                    record = new DocumentRecord(
                        NextId,
                        ArtView.ViewManager.Document,
                        _documentWatcher.OldProperties.ToDictionary(k => k.Key.property, k => k.Value),
                        _documentWatcher.NewProperties.ToDictionary(k => k.Key.property, k => k.Value),
                        desc);
                }

                _documentWatcher.Dispose();
                _documentWatcher = null;

                if (record == null) return;

                var removed = _redo.Reverse().ToList();

                _redo.Clear();

                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        removed, 0));

                _undo.Push(record);
                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add,
                        record, 0));

                RaisePropertyChanged(nameof(CurrentRecord));
                RaisePropertyChanged(nameof(NextId));
                RaisePropertyChanged(nameof(Time));
            }
        }

        public long Time
        {
            get => CurrentRecord?.Id ?? 0;
            set
            {
                while (value > Time && _redo.Count > 0)
                {
                    var record = _redo.Pop();
                    record.Apply(ArtView.ViewManager.Document);
                    _undo.Push(record);
                }

                while (value < Time && _undo.Count > 0)
                {
                    var record = _undo.Pop();
                    record.Revert(ArtView.ViewManager.Document);
                    _redo.Push(record);
                }

                RaisePropertyChanged(nameof(CurrentRecord));
                RaisePropertyChanged(nameof(NextId));
                RaisePropertyChanged(nameof(Time));
                TimeChanged?.BeginInvoke(this, value, null, null);
            }
        }

        #endregion

        public event EventHandler<long> TimeChanged;

        public IEnumerator<IRecord<long>> GetEnumerator()
        {
            return _redo.Reverse().Concat(_undo).GetEnumerator();
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

    public class LayerRecord : IHistoryRecord<long, ILayer>
    {
        private readonly Dictionary<Type, TypeAccessor> _accessors = new Dictionary<Type, TypeAccessor>();
        private readonly Dictionary<(Guid id, string name), object> _newProperties;
        private readonly Dictionary<(Guid id, string name), object> _oldProperties;

        private LayerRecord(long id, Guid[] targets)
        {
            Id = id;
            Targets = targets;

            if (targets.Length < 1)
                throw new ArgumentException("Cannot create a record that has no target.");
        }

        public LayerRecord(long id, Guid[] targets,
            Dictionary<(Guid, string), object> oldProps,
            Dictionary<(Guid, string), object> newProps) :
            this(id, targets)
        {
            _oldProperties = oldProps;
            _newProperties = newProps;
            Description = $"Changed {string.Join(", ", newProps.Keys.Select(k => k.Item2))} of " +
                          $"{targets.Length} layer{(targets.Length > 1 ? "s" : "")}";
        }

        public LayerRecord(long id, Guid[] targets,
            Dictionary<(Guid, string), object> oldProps,
            Dictionary<(Guid, string), object> newProps,
            string desc) :
            this(id, targets)
        {
            _oldProperties = oldProps;
            _newProperties = newProps;
            Description = desc;
        }

        public Guid LayerId => Targets[0];

        public IReadOnlyDictionary<(Guid id, string name), object> NewProperties => _newProperties;

        public IReadOnlyDictionary<(Guid id, string name), object> OldProperties => _oldProperties;

        public Guid[] Targets { get; }

        #region IHistoryRecord<long,ILayer> Members

        public string Description { get; }

        public long Time { get; } = Service.Time.Now;

        public void Apply(ILayer layer)
        {
            Apply(layer, NewProperties);
        }

        public void Revert(ILayer layer)
        {
            Apply(layer, OldProperties);
        }

        void IRecord<long>.Apply(object target)
        {
            if (target is ILayer layer)
                Apply(layer);

            if (target is Document doc)
                Apply(doc.Root);
        }

        public long Id { get; }

        IDictionary IRecord<long>.NewProperties => _newProperties;

        IDictionary IRecord<long>.OldProperties => _oldProperties;

        void IRecord<long>.Revert(object target)
        {
            if (target is ILayer layer)
                Revert(layer);

            if (target is Document doc)
                Revert(doc.Root);
        }

        #endregion

        private void Apply(ILayer layer, IReadOnlyDictionary<(Guid id, string name), object> properties)
        {
            foreach (var prop in properties)
            {
                var target = layer.Find(prop.Key.id);
                var type = target.GetType();

                if (!_accessors.TryGetValue(type, out var accessor))
                    accessor = _accessors[type] = TypeAccessor.Create(type);

                if (prop.Key.name == nameof(Layer.Parent))
                {
                    target.Parent?.Remove(target);

                    var targetParent = (IContainerLayer) layer.Find(((IContainerLayer) prop.Value).Id);
                    targetParent?.Add(target);
                }
                else
                {
                    accessor[target, prop.Key.name] = prop.Value;
                }
            }
        }
    }

    public class DocumentRecord : IHistoryRecord<long, Document>
    {
        private readonly TypeAccessor _accessor = TypeAccessor.Create(typeof(Document));
        private readonly Dictionary<string, object> _newProperties;
        private readonly Dictionary<string, object> _oldProperties;

        public DocumentRecord(long id, Document target,
            Dictionary<string, object> oldProps,
            Dictionary<string, object> newProps,
            string desc)
        {
            Id = id;
            Target = target;
            _oldProperties = oldProps;
            _newProperties = newProps;
            Description = desc;
        }

        public IReadOnlyDictionary<string, object> NewProperties => _newProperties;

        public IReadOnlyDictionary<string, object> OldProperties => _oldProperties;

        public Document Target { get; }

        #region IHistoryRecord<long,Document> Members

        public string Description { get; }
        public long Time { get; } = Service.Time.Now;

        public void Apply(Document doc)
        {
            Apply(doc, NewProperties);
        }

        public void Revert(Document doc)
        {
            Apply(doc, OldProperties);
        }

        void IRecord<long>.Apply(object target)
        {
            if (target is Document layer)
                Apply(layer);
        }

        public long Id { get; }

        IDictionary IRecord<long>.NewProperties => _newProperties;

        IDictionary IRecord<long>.OldProperties => _oldProperties;

        void IRecord<long>.Revert(object target)
        {
            if (target is Document layer)
                Revert(layer);
        }

        #endregion

        private void Apply(Document doc, IReadOnlyDictionary<string, object> properties)
        {
            foreach (var prop in properties)
                _accessor[doc, prop.Key] = prop.Value;
        }
    }
}