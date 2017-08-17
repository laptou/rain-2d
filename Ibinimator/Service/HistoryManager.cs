﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FastMember;
using Ibinimator.View.Control;
using SharpDX;
using Ibinimator.Model;
using Ibinimator.Shared;
using Ibinimator.ViewModel;

namespace Ibinimator.Service
{
    public class Watcher<TK, T> : IDisposable where T : INotifyPropertyChanging, INotifyPropertyChanged
    {
        static Watcher()
        {
            _animatable =
                (from p in typeof(T).GetProperties()
                where p.GetCustomAttributes(typeof(AnimatableAttribute), false).Any()
                select p.Name).ToList();
            _undoable =
            (from p in typeof(T).GetProperties()
                where p.GetCustomAttributes(typeof(UndoableAttribute), false).Any()
                select p.Name).ToList();
        }

        // ReSharper disable once StaticMemberInGenericType
        private static List<string> _animatable;
        // ReSharper disable once StaticMemberInGenericType
        private static List<string> _undoable;
        private readonly Func<T, TK> _keySelector;
        private readonly TypeAccessor _accessor = TypeAccessor.Create(typeof(T));
        private readonly Dictionary<(TK id, string property), object> _oldProperties = new Dictionary<(TK, string), object>();
        private readonly Dictionary<(TK id, string property), object> _newProperties = new Dictionary<(TK, string), object>();

        public T Target { get; }
        public bool RecordAnimatable { get; set; } = true;
        public bool RecordUndoable { get; set; } = false;
        public IReadOnlyDictionary<(TK id, string property), object> OldProperties => _oldProperties;
        public IReadOnlyDictionary<(TK id, string property), object> NewProperties => _newProperties;

        public Watcher(T target, Func<T, TK> keySelector)
        {
            _keySelector = keySelector;
            Target = target;
            Target.PropertyChanging += TargetOnPropertyChanging;
            Target.PropertyChanged += TargetOnPropertyChanged;
        }

        private void TargetOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(RecordAnimatable && _animatable.Contains(e.PropertyName) || 
                RecordUndoable && _undoable.Contains(e.PropertyName))
            {
                _newProperties[(_keySelector((T) sender), e.PropertyName)] = 
                    _accessor[sender, e.PropertyName];
            }
        }

        private void TargetOnPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (RecordAnimatable && _animatable.Contains(e.PropertyName) ||
                RecordUndoable && _undoable.Contains(e.PropertyName))
            {
                var key = (_keySelector((T)sender), e.PropertyName);
                if (!_oldProperties.ContainsKey(key))
                    _oldProperties[key] = _accessor[sender, e.PropertyName];
            }
        }

        public void Dispose()
        {
            Target.PropertyChanging -= TargetOnPropertyChanging;
            Target.PropertyChanged -= TargetOnPropertyChanged;
        }
    }

    public class HistoryManager : Model.Model, IHistoryManager
    {
        private readonly Stack<HistoryRecord> _undo = new Stack<HistoryRecord>();
        private readonly Stack<HistoryRecord> _redo = new Stack<HistoryRecord>();

        public HistoryManager(ArtView artView)
        {
            ArtView = artView;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event EventHandler<long> TimeChanged;

        public ArtView ArtView { get; }

        public IRecord<long> CurrentRecord => _undo.Count > 0 ? _undo.Peek() : null;

        public long Time
        {
            get => CurrentRecord?.Id ?? 0;
            set
            {
                while (value > Time && _redo.Count > 0)
                {
                    var record = _redo.Pop();
                    record.Apply(ArtView.ViewManager.Root);
                    _undo.Push(record);
                }

                while (value < Time && _undo.Count > 0)
                {
                    var record = _undo.Pop();
                    record.Revert(ArtView.ViewManager.Root);
                    _redo.Push(record);
                }

                RaisePropertyChanged(nameof(CurrentRecord));
                RaisePropertyChanged(nameof(NextId));
                RaisePropertyChanged(nameof(Time));
                TimeChanged?.BeginInvoke(this, value, null, null);
            }
        }

        public long NextId => Time + 1;

        public IEnumerator<IRecord<long>> GetEnumerator()
        {
            return _redo.Reverse().Concat(_undo).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Base(Layer root)
        {
            _undo.Clear();
            _redo.Clear();
            Time = 0;
        }

        public void Key<TV>(Layer layer, long time, string property, TV value)
        {
            Key(layer.Id, time, property, value);
        }

        public void Key<TV>(Guid id, long time, string property, TV value)
        {
            throw new NotSupportedException();
        }

        void IRecorder<long>.Key(IRecord<long> record)
        {
            Key((HistoryRecord)record);
        }

        public void Key(HistoryRecord record)
        {
            var removed = _redo.ToList();
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
            RaisePropertyChanged(nameof(Time));
            TimeChanged?.Invoke(this, Time);
        }

        public Watcher<Guid, Layer> BeginRecord(Layer root)
        {
            return new Watcher<Guid, Layer>(root, l => l.Id);
        }

        public HistoryRecord EndRecord(Watcher<Guid, Layer> watcher, long time)
        {
            var ids = watcher.NewProperties.Keys.Select(k => k.id).Distinct().ToArray();

            using (watcher)
                return ids.Any() ? new HistoryRecord(time, ids, watcher.OldProperties, watcher.NewProperties) : null;
        }

        IRecord<long> IRecorder<long>.EndRecord(Watcher<Guid, Layer> watcher, long time) => 
            EndRecord(watcher, time);

        public void Undo()
        {
            Time--;
        }

        public void Redo()
        {
            Time++;
        }
    }

    public class HistoryRecord : IRecord<long>
    {
        private readonly TypeAccessor _layerAccessor = TypeAccessor.Create(typeof(Layer));
        
        public Guid[] Targets { get; }

        public string Description { get; }

        private HistoryRecord(long id, Guid[] targets)
        {
            Id = id;
            Targets = targets;

            if (targets.Length < 1)
                throw new ArgumentException("Cannot create a record that has no target.");
        }

        public HistoryRecord(long id, Guid[] targets, IReadOnlyDictionary<(Guid, string), object> oldProps, IReadOnlyDictionary<(Guid, string), object> newProps) :
            this(id, targets)
        {
            OldProperties = oldProps;
            NewProperties = newProps;
            Description = $"Changed {string.Join(", ", newProps.Keys.Select(k => k.Item2))} of " +
                          $"{targets.Length} layer{(targets.Length > 1 ? "s" : "")}";
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }

        public Guid LayerId => Targets[0];

        public IReadOnlyDictionary<(Guid id, string name), object> NewProperties { get; }

        public IReadOnlyDictionary<(Guid id, string name), object> OldProperties { get; }

        public void Apply(Layer layer)
        {
            Apply(layer, NewProperties);
        }

        private void Apply(Layer layer, IReadOnlyDictionary<(Guid id, string name), object> properties)
        {
            foreach (var prop in properties)
            {
                if (prop.Key.name == nameof(Layer.Parent))
                {
                    var target = layer.Find(prop.Key.id);

                    target.Parent?.Remove(target);
                    ((Layer) prop.Value)?.Add(target);
                }
                else
                {
                    _layerAccessor[layer.Find(prop.Key.id), prop.Key.name] = prop.Value;
                }
            }
        }

        public void Revert(Layer layer)
        {
            Apply(layer, OldProperties);
        }

        public long Id { get; }
    }

    public static class HistoryCommands
    {
        public static readonly DelegateCommand<IHistoryManager> UndoCommand = new DelegateCommand<IHistoryManager>(Undo, null);
        public static readonly DelegateCommand<IHistoryManager> RedoCommand = new DelegateCommand<IHistoryManager>(Redo, null);

        private static void Undo(IHistoryManager historyManager)
        {
            historyManager.Undo();
        }

        private static void Redo(IHistoryManager historyManager)
        {
            historyManager.Redo();
        }
    }
}