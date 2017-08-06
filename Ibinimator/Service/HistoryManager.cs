using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.View.Control;
using SharpDX;
using Ibinimator.Model;
using Ibinimator.Shared;
using Ibinimator.ViewModel;

namespace Ibinimator.Service
{
    public class HistoryManager : Model.Model, IHistoryManager, IEnumerable<IHistoryRecord>
    {
        private readonly Stack<IHistoryRecord> _backRecords = new Stack<IHistoryRecord>();
        private readonly Stack<IHistoryRecord> _fwdRecords = new Stack<IHistoryRecord>();
        private long _id;

        public HistoryManager(ArtView artView)
        {
            ArtView = artView;
        }

        public ArtView ArtView { get; }

        public T Create<T>() where T: IHistoryRecord
        {
            return (T) Activator.CreateInstance(typeof(T),  _id++);
        }

        public void Record(IHistoryRecord record)
        {
            _backRecords.Push(record);
            _fwdRecords.Clear();
        }

        public void Undo()
        {
            if (_backRecords.Count < 1) return;

            var record = _backRecords.Pop();

            record.Undo();

            _fwdRecords.Push(record);

            StackTraversed?.Invoke(this, null);
        }

        public void Redo()
        {
            if (_fwdRecords.Count < 1) return;

            var record = _fwdRecords.Pop();

            record.Do();

            _backRecords.Push(record);

            StackTraversed?.Invoke(this, null);
        }

        public event EventHandler StackTraversed;

        public IEnumerator<IHistoryRecord> GetEnumerator()
        {
            return Enumerable.Concat(_fwdRecords.Reverse(), _backRecords).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class TransformHistoryRecord : IHistoryRecord
    {
        public long Id { get; }

        public HistoryRecordType Type => HistoryRecordType.Transform;

        object IHistoryRecord.Data => Data;

        public Matrix3x2 Data { get; set; } = Matrix3x2.Identity;

        object IHistoryRecord.Target => Targets;

        public string Description => 
            $"Transform {Targets.Length} {(Targets.Length > 1 ? "layers" : "layer")}";

        public Layer[] Targets { get; set; } = new Layer[0];

        public TransformHistoryRecord(long id)
        {
            Id = id;
        }

        public void Do()
        {
            foreach (var target in Targets)
            {
                lock (target)
                {
                    var layerTransform =
                        target.AbsoluteTransform * Data * Matrix3x2.Invert(target.WorldTransform);
                    var delta = layerTransform.Decompose();

                    target.Scale = delta.scale;
                    target.Rotation = delta.rotation;
                    target.Position = delta.translation;
                    target.Shear = delta.skew;

                    target.UpdateTransform();
                }
            }
            
        }

        public void Undo()
        {
            foreach (var target in Targets)
            {
                lock (target)
                {
                    var layerTransform =
                        target.AbsoluteTransform * Matrix3x2.Invert(Data) * Matrix3x2.Invert(target.WorldTransform);
                    var delta = layerTransform.Decompose();

                    target.Scale = delta.scale;
                    target.Rotation = delta.rotation;
                    target.Position = delta.translation;
                    target.Shear = delta.skew;

                    target.UpdateTransform();
                }
            }
        }
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